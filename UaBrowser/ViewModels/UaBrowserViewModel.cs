// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.UaBrowser.ViewModels
{
    [Export]
    public class UaBrowserViewModel : INotifyPropertyChanged, IDisposable
    {
        public const string VsCMLanguageCSharp = "{B5E9BD34-6D3E-4B5D-925E-8A43B79820B4}";
        public const string VsCMLanguageVB = "{B5E9BD33-6D3E-4B5D-925E-8A43B79820B4}";

        private const string CollectionPath = "UaBrowser";
        private const string EventDataTypeCS = "BaseEvent";
        private const string EventDataTypeVB = "BaseEvent";
        private const string MethodDataTypeCS = "Task<object[]>";
        private const string MethodDataTypeVB = "Task(Of Object())";

        private static readonly QualifiedName DefaultBinary = QualifiedName.Parse("0:Default Binary");
        private static readonly NodeId OpcBinarySchema = NodeId.Parse(ObjectIds.OPCBinarySchema_TypeSystem);
        private static readonly NodeId XmlSchema = NodeId.Parse(ObjectIds.XmlSchema_TypeSystem);

        private CancellationTokenSource cts;
        private string endpointUrl;
        private string eventFormatBasic;
        private string eventFormatCSharp;
        private ObservableCollection<string> history;
        private EnvDTE.DTE ide;
        private SemaphoreSlim @lock;
        private ObservableCollection<ReferenceDescriptionViewModel> namespaceItems;
        private string readOnlyValueFormatBasic;
        private string readOnlyValueFormatCSharp;
        private UaTcpSessionChannel channel;
        private WritableSettingsStore store;
        private string valueFormatBasic;
        private string valueFormatCSharp;
        private string methodFormatBasic;
        private string methodFormatCSharp;
        private string structureFormatBasic;
        private string structureFormatCSharp;
        private ApplicationDescription localDescription;
        private IUserIdentity userIdentity;
        private string userName;
        private string password;
        private bool showingLoginPanel;
        private Dictionary<ExpandedNodeId, string> typeCache;
        private Dictionary<ExpandedNodeId, (XElement Dictionary, string TargetNamespace)> dictionaryCache;

        public UaBrowserViewModel()
        {
            this.showingLoginPanel = true;
        }

        [ImportingConstructor]
        public UaBrowserViewModel(Microsoft.VisualStudio.Shell.SVsServiceProvider vsServiceProvider)
        {
            this.cts = new CancellationTokenSource();
            this.@lock = new SemaphoreSlim(1);

            // describe self.
            this.localDescription = new ApplicationDescription()
            {
                ApplicationName = "Workstation.UaBrowser",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaBrowser",
                ApplicationType = ApplicationType.Client
            };

            this.BrowseStopCommand = new DelegateCommand(this.BrowseStop);
            this.SaveSettingsCommand = new DelegateCommand(this.SaveSettings);
            this.ResetSettingsCommand = new DelegateCommand(this.ResetSettings);
            this.NamespaceItems = new ObservableCollection<ReferenceDescriptionViewModel>();
            this.ide = (EnvDTE.DTE)vsServiceProvider.GetService(typeof(EnvDTE.DTE));
            var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
            this.store = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            this.LoadSettings();
            this.LoadHistory();
            this.CertificateStore = new DirectoryStore(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Workstation.UaBrowser\pki"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string EndpointUrl
        {
            get
            {
                return this.endpointUrl;
            }

            set
            {
                this.endpointUrl = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowingLoginPanel
        {
            get
            {
                return this.showingLoginPanel;
            }
        }

        public string ValueFormatBasic
        {
            get
            {
                return this.valueFormatBasic;
            }

            set
            {
                this.valueFormatBasic = value;
                this.NotifyPropertyChanged();
            }
        }

        public string ValueFormatCSharp
        {
            get
            {
                return this.valueFormatCSharp;
            }

            set
            {
                this.valueFormatCSharp = value;
                this.NotifyPropertyChanged();
            }
        }

        public string ReadOnlyValueFormatBasic
        {
            get
            {
                return this.readOnlyValueFormatBasic;
            }

            set
            {
                this.readOnlyValueFormatBasic = value;
                this.NotifyPropertyChanged();
            }
        }

        public string ReadOnlyValueFormatCSharp
        {
            get
            {
                return this.readOnlyValueFormatCSharp;
            }

            set
            {
                this.readOnlyValueFormatCSharp = value;
                this.NotifyPropertyChanged();
            }
        }

        public string StructureFormatBasic
        {
            get
            {
                return this.structureFormatBasic;
            }

            set
            {
                this.structureFormatBasic = value;
                this.NotifyPropertyChanged();
            }
        }

        public string StructureFormatCSharp
        {
            get
            {
                return this.structureFormatCSharp;
            }

            set
            {
                this.structureFormatCSharp = value;
                this.NotifyPropertyChanged();
            }
        }

        public string EventFormatBasic
        {
            get
            {
                return this.eventFormatBasic;
            }

            set
            {
                this.eventFormatBasic = value;
                this.NotifyPropertyChanged();
            }
        }

        public string EventFormatCSharp
        {
            get
            {
                return this.eventFormatCSharp;
            }

            set
            {
                this.eventFormatCSharp = value;
                this.NotifyPropertyChanged();
            }
        }

        public string MethodFormatBasic
        {
            get
            {
                return this.methodFormatBasic;
            }

            set
            {
                this.methodFormatBasic = value;
                this.NotifyPropertyChanged();
            }
        }

        public string MethodFormatCSharp
        {
            get
            {
                return this.methodFormatCSharp;
            }

            set
            {
                this.methodFormatCSharp = value;
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> History
        {
            get
            {
                return this.history;
            }

            private set
            {
                this.history = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get { return this.@lock.CurrentCount == 0; }
        }

        public ObservableCollection<ReferenceDescriptionViewModel> NamespaceItems
        {
            get
            {
                return this.namespaceItems;
            }

            private set
            {
                this.namespaceItems = value;
                this.NotifyPropertyChanged();
            }
        }

        public ICommand ResetSettingsCommand { get; private set; }

        public ICommand SaveSettingsCommand { get; private set; }

        public ICommand BrowseStopCommand { get; private set; }

        public ICertificateStore CertificateStore
        {
            get;
        }

        public void Dispose()
        {
            this.SaveHistory();
            if (this.channel != null)
            {
                try
                {
                    this.channel.CloseAsync().Wait();
                }
                catch (Exception)
                {
                    this.channel.AbortAsync().Wait();
                }
            }

            if (this.cts != null)
            {
                this.cts.Cancel();
                this.cts.Dispose();
                this.cts = null;
            }
        }

        public string GetSnippet(ReferenceDescriptionViewModel vm)
        {
            var fcm = this.ide?.ActiveDocument?.ProjectItem?.FileCodeModel;
            var language = fcm?.Language ?? VsCMLanguageCSharp;

            switch (language)
            {
                case VsCMLanguageCSharp:
                    if (vm is PropertyDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.ValueFormatCSharp, language);
                    }

                    if (vm is ReadonlyPropertyDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.ReadOnlyValueFormatCSharp, language);
                    }

                    if (vm is MethodDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.MethodFormatCSharp, language);
                    }

                    if (vm is EventDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.EventFormatCSharp, language);
                    }

                    if (vm is DataTypeDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.StructureFormatCSharp, language);
                    }

                    break;

                case VsCMLanguageVB:
                    if (vm is PropertyDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.ValueFormatBasic, language);
                    }

                    if (vm is ReadonlyPropertyDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.ReadOnlyValueFormatBasic, language);
                    }

                    if (vm is MethodDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.MethodFormatBasic, language);
                    }

                    if (vm is EventDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.EventFormatBasic, language);
                    }

                    if (vm is DataTypeDescriptionViewModel)
                    {
                        return vm.GetSnippet(this.StructureFormatBasic, language);
                    }

                    break;
            }

            return string.Empty;
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string FormatNodeId(ExpandedNodeId n, string language)
        {
            if (ExpandedNodeId.IsNull(n))
            {
                return string.Empty;
            }

            var s = n.ToString();
            switch (language)
            {
                case VsCMLanguageCSharp:
                    return s.Replace("\"", "\\\"");

                case VsCMLanguageVB:
                    return s.Replace("\"", "\"\"");
            }

            return string.Empty;
        }

        private static string FormatTypeName(Type t, string language)
        {
            if (t == null)
            {
                return string.Empty;
            }

            if (t.IsArray)
            {
                switch (language)
                {
                    case VsCMLanguageCSharp:
                        return t.GetElementType().Name + "[]";

                    case VsCMLanguageVB:
                        return t.GetElementType().Name + "()";
                }
            }

            if (!t.IsGenericType)
            {
                return t.Name;
            }

            if (t.IsNested && t.DeclaringType.IsGenericType)
            {
                throw new NotImplementedException();
            }

            StringBuilder txt = new StringBuilder();
            switch (language)
            {
                case VsCMLanguageCSharp:
                    txt.Append(t.Name, 0, t.Name.IndexOf('`'));
                    txt.Append("<");
                    txt.Append(string.Join(", ", t.GetGenericArguments().Select(arg => FormatTypeName(arg, language))));
                    txt.Append(">");
                    return txt.ToString();

                case VsCMLanguageVB:
                    txt.Append(t.Name, 0, t.Name.IndexOf('`'));
                    txt.Append("(Of ");
                    txt.Append(string.Join(", ", t.GetGenericArguments().Select(arg => FormatTypeName(arg, language))));
                    txt.Append(")");
                    return txt.ToString();
            }

            return t.Name;
        }

        private async Task LoadChildrenAsync(ReferenceDescriptionViewModel parent)
        {
            try
            {
                var token = this.cts.Token;
                await this.@lock.WaitAsync(token);
                this.NotifyPropertyChanged("IsLoading");
                try
                {
                    do
                    {
                        parent.Children.Clear();
                        try
                        {
                            if (this.channel == null || this.channel.State != CommunicationState.Opened)
                            {
                                var getEndpointsRequest = new GetEndpointsRequest
                                {
                                    EndpointUrl = this.endpointUrl,
                                    ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                                };
                                var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest);
                                token.ThrowIfCancellationRequested();
                                var selectedEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).First();
                                if (selectedEndpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.Anonymous))
                                {
                                    this.HideLoginPanel();
                                    this.userIdentity = new AnonymousIdentity();
                                }
                                else if (selectedEndpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.UserName))
                                {
                                    if (!this.showingLoginPanel)
                                    {
                                        this.ShowLoginPanel();
                                        return;
                                    }
                                    else if (!this.ValidateLoginCredentials())
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        this.userIdentity = new UserNameIdentity(this.userName, this.password);
                                    }
                                }
                                else
                                {
                                    throw new NotImplementedException("Browser supports only UserName and Anonymous identity, for now.");
                                }

                                this.channel = new UaTcpSessionChannel(
                                    this.localDescription,
                                    this.CertificateStore,
                                    this.userIdentity,
                                    selectedEndpoint);
                                await this.channel.OpenAsync();
                            }

                            token.ThrowIfCancellationRequested();
                            var rds = new List<ReferenceDescription>();
                            var browseRequest = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(parent.NodeId, this.channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HierarchicalReferences), ResultMask = (uint)BrowseResultMask.TargetInfo, NodeClassMask = (uint)NodeClass.Unspecified, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = true } }, RequestedMaxReferencesPerNode = 128 };
                            var browseResponse = await this.channel.BrowseAsync(browseRequest);
                            rds.AddRange(browseResponse.Results.Where(result => result.References != null).SelectMany(result => result.References));
                            var continuationPoints = browseResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
                            while (continuationPoints.Length > 0)
                            {
                                token.ThrowIfCancellationRequested();
                                var browseNextRequest = new BrowseNextRequest { ContinuationPoints = continuationPoints, ReleaseContinuationPoints = false };
                                var browseNextResponse = await this.channel.BrowseNextAsync(browseNextRequest);
                                rds.AddRange(browseNextResponse.Results.Where(result => result.References != null).SelectMany(result => result.References));
                                continuationPoints = browseNextResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
                            }

                            if (rds.Count == 0)
                            {
                                return;
                            }

                            foreach (var rd in rds)
                            {
                                token.ThrowIfCancellationRequested();

                                var n = ExpandedNodeId.ToNodeId(rd.NodeId, this.channel.NamespaceUris);
                                ReadRequest readRequest = null;
                                ReadResponse readResponse = null;

                                switch (rd.NodeClass)
                                {
                                    case NodeClass.Variable:

                                        readRequest = new ReadRequest
                                        {
                                            NodesToRead = new ReadValueId[]
                                            {
                                                new ReadValueId { NodeId = n, AttributeId = AttributeIds.DataType },
                                                new ReadValueId { NodeId = n, AttributeId = AttributeIds.ValueRank },
                                                new ReadValueId { NodeId = n, AttributeId = AttributeIds.UserAccessLevel }
                                            }
                                        };
                                        readResponse = await this.channel.ReadAsync(readRequest);
                                        var dataTypeNodeId = readResponse.Results[0].GetValueOrDefault(NodeId.Null);
                                        var valueRank = readResponse.Results[1].GetValueOrDefault(-1);
                                        var accessLevel = (AccessLevelFlags)Enum.ToObject(typeof(AccessLevelFlags), readResponse.Results[2].GetValueOrDefault<byte>());
                                        var dataType = await this.GetSystemTypeAsync(dataTypeNodeId, valueRank);

                                        if (accessLevel.HasFlag(AccessLevelFlags.CurrentWrite))
                                        {
                                            parent.Children.Add(new PropertyDescriptionViewModel(rd, dataType, parent, this.LoadChildrenAsync));
                                            break;
                                        }

                                        parent.Children.Add(new ReadonlyPropertyDescriptionViewModel(rd, dataType, parent, this.LoadChildrenAsync));
                                        break;

                                    case NodeClass.Object:

                                        if (n == OpcBinarySchema || n == XmlSchema)
                                        {
                                            continue;
                                        }

                                        readRequest = new ReadRequest
                                        {
                                            NodesToRead = new ReadValueId[]
                                            {
                                                new ReadValueId { NodeId = n, AttributeId = AttributeIds.EventNotifier },
                                            }
                                        };
                                        readResponse = await this.channel.ReadAsync(readRequest);
                                        var notifier = (EventNotifierFlags)Enum.ToObject(typeof(EventNotifierFlags), readResponse.Results[0].GetValueOrDefault<byte>());
                                        if (notifier.HasFlag(EventNotifierFlags.SubscribeToEvents))
                                        {
                                            parent.Children.Add(new EventDescriptionViewModel(rd, parent, this.LoadChildrenAsync));
                                            break;
                                        }

                                        parent.Children.Add(new ReferenceDescriptionViewModel(rd, parent, this.LoadChildrenAsync));
                                        break;

                                    case NodeClass.Method:

                                        var inArgs = new Parameter[0];
                                        var outArgs = new Parameter[0];
                                        var translateRequest = new TranslateBrowsePathsToNodeIdsRequest
                                        {
                                            BrowsePaths = new[]
                                            {
                                            new BrowsePath
                                            {
                                                StartingNode = n,
                                                RelativePath = new RelativePath { Elements = new[] { new RelativePathElement { TargetName = QualifiedName.Parse("0:InputArguments") } } }
                                            },
                                            new BrowsePath
                                            {
                                                StartingNode = n,
                                                RelativePath = new RelativePath { Elements = new[] { new RelativePathElement { TargetName = QualifiedName.Parse("0:OutputArguments") } } }
                                            }
                                        }
                                        };
                                        var translateResponse = await this.channel.TranslateBrowsePathsToNodeIdsAsync(translateRequest);

                                        if (StatusCode.IsGood(translateResponse.Results[0].StatusCode))
                                        {
                                            readRequest = new ReadRequest
                                            {
                                                NodesToRead = new ReadValueId[]
                                                {
                                                new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(translateResponse.Results[0].Targets[0].TargetId, this.channel.NamespaceUris), AttributeId = AttributeIds.Value },
                                                }
                                            };
                                            readResponse = await this.channel.ReadAsync(readRequest);

                                            if (StatusCode.IsGood(readResponse.Results[0].StatusCode))
                                            {
                                                var plist = new List<Parameter>();
                                                foreach (var a in readResponse.Results[0].GetValueOrDefault<object[]>().Cast<Argument>())
                                                {
                                                    var t = await this.GetSystemTypeAsync(a.DataType, a.ValueRank);
                                                    var p = new Parameter(t, a.Name, a.Description.Text);
                                                    plist.Add(p);
                                                }

                                                inArgs = plist.ToArray();
                                            }
                                        }

                                        if (StatusCode.IsGood(translateResponse.Results[1].StatusCode))
                                        {
                                            readRequest = new ReadRequest
                                            {
                                                NodesToRead = new ReadValueId[]
                                                {
                                                new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(translateResponse.Results[1].Targets[0].TargetId, this.channel.NamespaceUris), AttributeId = AttributeIds.Value },
                                                }
                                            };
                                            readResponse = await this.channel.ReadAsync(readRequest);

                                            if (StatusCode.IsGood(readResponse.Results[0].StatusCode))
                                            {
                                                var plist = new List<Parameter>();
                                                foreach (var a in readResponse.Results[0].GetValueOrDefault<object[]>().Cast<Argument>())
                                                {
                                                    var t = await this.GetSystemTypeAsync(a.DataType, a.ValueRank);
                                                    var p = new Parameter(t, a.Name, a.Description.Text);
                                                    plist.Add(p);
                                                }

                                                outArgs = plist.ToArray();
                                            }
                                        }

                                        parent.Children.Add(new MethodDescriptionViewModel(rd, inArgs, outArgs, parent, this.LoadChildrenAsync));
                                        break;

                                    case NodeClass.DataType:

                                        // first check if IEncodable
                                        var browseRequest3 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(rd.NodeId, this.channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasEncoding), ResultMask = (uint)BrowseResultMask.BrowseName, NodeClassMask = (uint)NodeClass.Object, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = false } } };
                                        var browseResponse3 = await this.channel.BrowseAsync(browseRequest3);
                                        var encodingRef = browseResponse3.Results[0].References?.FirstOrDefault(r => r.BrowseName == DefaultBinary);

                                        if (encodingRef != null)
                                        {
                                            // follow to DataDescription
                                            var browseRequest4 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(encodingRef.NodeId, this.channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasDescription), ResultMask = (uint)BrowseResultMask.BrowseName, NodeClassMask = (uint)NodeClass.Variable, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = false } } };
                                            var browseResponse4 = await this.channel.BrowseAsync(browseRequest4);
                                            var descriptionRef = browseResponse4.Results[0].References?.FirstOrDefault();
                                            if (descriptionRef != null)
                                            {
                                                var readRequest5 = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(descriptionRef.NodeId, this.channel.NamespaceUris), AttributeId = AttributeIds.Value } } };
                                                var readResponse5 = await this.channel.ReadAsync(readRequest5);

                                                var dataType5 = readResponse5.Results[0].GetValueOrDefault<string>();

                                                // browse to dictionary
                                                var browseRequest6 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(descriptionRef.NodeId, this.channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasComponent), ResultMask = (uint)BrowseResultMask.None, NodeClassMask = (uint)NodeClass.Variable, BrowseDirection = BrowseDirection.Inverse, IncludeSubtypes = false } } };
                                                var browseResponse6 = await this.channel.BrowseAsync(browseRequest6);
                                                var dictionaryRef = browseResponse6.Results[0].References?.FirstOrDefault();
                                                if (dictionaryRef != null)
                                                {
                                                    LazyInitializer.EnsureInitialized(ref this.dictionaryCache);
                                                    if (!this.dictionaryCache.TryGetValue(dictionaryRef.NodeId, out (XElement Dictionary, string TargetNamespace) tuple7))
                                                    {
                                                        var readRequest7 = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(dictionaryRef.NodeId, this.channel.NamespaceUris), AttributeId = AttributeIds.Value } } };
                                                        var readResponse7 = await this.channel.ReadAsync(readRequest7);

                                                        var array7 = readResponse7.Results[0].GetValueOrDefault<byte[]>();
                                                        var dictionary = XElement.Parse(Encoding.UTF8.GetString(array7, 0, array7.Length).TrimEnd('\0'));
                                                        var targetNamespace = (string)dictionary.Attribute("TargetNamespace");
                                                        tuple7 = (dictionary, targetNamespace);
                                                        this.dictionaryCache.Add(dictionaryRef.NodeId, tuple7);
                                                    }

                                                    var element7 = tuple7.Dictionary?.Elements().FirstOrDefault(el => (string)el.Attribute("Name") == dataType5);
                                                    if (element7 != null)
                                                    {
                                                        parent.Children.Add(new DataTypeDescriptionViewModel(rd, encodingRef.NodeId, element7, tuple7.TargetNamespace, parent, this.LoadChildrenAsync));
                                                        break;
                                                    }
                                                }
                                            }
                                        }


                                        parent.Children.Add(new ReferenceDescriptionViewModel(rd, parent, this.LoadChildrenAsync));

                                        break;

                                    default:

                                        parent.Children.Add(new ReferenceDescriptionViewModel(rd, parent, this.LoadChildrenAsync));
                                        break;
                                }

                                await Task.Yield();
                            }

                            break; // exit while;
                        }
                        catch (OperationCanceledException)
                        {
                            // exit while;
                        }
                        catch (ServiceResultException ex)
                        {
                            Trace.TraceInformation("ServiceResultException: {0}", ex);
                            if (this.channel != null)
                            {
                                await this.channel.AbortAsync(token);
                                this.channel = null;
                            }

                            if (ex.HResult == unchecked((int)StatusCodes.BadSessionIdInvalid))
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation("Exception {0}", ex);
                            if (this.channel != null)
                            {
                                await this.channel.AbortAsync(token);
                                this.channel = null;
                            }
                        }

                        try
                        {
                            await Task.Delay(5000, token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    while (!token.IsCancellationRequested);
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Exception {0}", ex);
                }
                finally
                {
                    this.@lock.Release();
                    this.NotifyPropertyChanged("IsLoading");
                }
            }
            catch (OperationCanceledException)
            {
                // only get here if cancelled while waiting for lock
            }
        }

        private async Task<string> GetSystemTypeAsync(NodeId dataTypeNode, int valueRank = -1)
        {
            string dataType;
            ExpandedNodeId dataTypeId, origDataTypeId;

            if (dataTypeNode != NodeId.Null)
            {
                dataTypeId = origDataTypeId = NodeId.ToExpandedNodeId(dataTypeNode, this.channel.NamespaceUris);

                if (!this.typeCache.TryGetValue(dataTypeId, out dataType))
                {
                    // first check if IEncodable
                    var browseRequest = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = dataTypeNode, ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasEncoding), ResultMask = (uint)BrowseResultMask.BrowseName, NodeClassMask = (uint)NodeClass.Object, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = false } } };
                    var browseResponse = await this.channel.BrowseAsync(browseRequest);
                    var encodingRef = browseResponse.Results[0].References?.FirstOrDefault(r => r.BrowseName == DefaultBinary);

                    if (encodingRef != null)
                    {
                        // follow to DataDescription
                        var browseRequest2 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(encodingRef.NodeId, this.channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasDescription), ResultMask = (uint)BrowseResultMask.BrowseName, NodeClassMask = (uint)NodeClass.Variable, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = false } } };
                        var browseResponse2 = await this.channel.BrowseAsync(browseRequest2);
                        var descriptionRef = browseResponse2.Results[0].References?.FirstOrDefault();
                        if (descriptionRef != null)
                        {
                            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(descriptionRef.NodeId, this.channel.NamespaceUris), AttributeId = AttributeIds.Value } } };
                            var readResponse = await this.channel.ReadAsync(readRequest);

                            dataType = readResponse.Results[0].GetValueOrDefault<string>();
                        }
                    }

                    // find base type
                    if (dataType == null)
                    {
                        do
                        {
                            dataTypeNode = ExpandedNodeId.ToNodeId(dataTypeId, this.channel.NamespaceUris);
                            var browseRequest3 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = dataTypeNode, ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasSubtype), ResultMask = (uint)BrowseResultMask.None, NodeClassMask = (uint)NodeClass.DataType, BrowseDirection = BrowseDirection.Inverse, IncludeSubtypes = false } } };
                            var browseResponse3 = await this.channel.BrowseAsync(browseRequest3);
                            var dataTypeRef = browseResponse3.Results[0].References?.FirstOrDefault();
                            dataTypeId = dataTypeRef?.NodeId;
                        }
                        while (dataTypeId != null && !this.typeCache.TryGetValue(dataTypeId, out dataType));
                    }

                    if (dataType == null)
                    {
                        dataType = typeof(object).Name;
                    }

                    this.typeCache.Add(origDataTypeId, dataType);
                }

                if (valueRank == 1)
                {
                    dataType = dataType + "[]";
                }

                if (valueRank > 1)
                {
                    dataType = dataType + "[" + new string(',', valueRank - 1) + "]";
                }
            }
            else
            {
                dataType = typeof(object).Name;
            }

            return dataType;
        }

        private bool ValidateLoginCredentials()
        {
            var isValid = !string.IsNullOrEmpty(this.userName);
            return isValid;
        }

        private void ShowLoginPanel()
        {
            if (!this.showingLoginPanel)
            {
                this.showingLoginPanel = true;
                this.NotifyPropertyChanged("ShowingLoginPanel");
            }
        }

        private void HideLoginPanel()
        {
            if (this.showingLoginPanel)
            {
                this.showingLoginPanel = false;
                this.NotifyPropertyChanged("ShowingLoginPanel");
            }
        }

        private void LoadHistory()
        {
            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.History)))
            {
                string value = this.store.GetString(CollectionPath, nameof(this.History));
                this.History = new ObservableCollection<string>(value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                this.History = new ObservableCollection<string>(new string[] { "opc.tcp://localhost:26543" });
            }
        }

        private void LoadSettings()
        {
            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.ValueFormatCSharp)))
            {
                this.ValueFormatCSharp = this.store.GetString(CollectionPath, nameof(this.ValueFormatCSharp));
            }
            else
            {
                this.ValueFormatCSharp = Workstation.UaBrowser.Properties.Resources.ValueFormatCSharp;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.EventFormatCSharp)))
            {
                this.EventFormatCSharp = this.store.GetString(CollectionPath, nameof(this.EventFormatCSharp));
            }
            else
            {
                this.EventFormatCSharp = Workstation.UaBrowser.Properties.Resources.EventFormatCSharp;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.MethodFormatCSharp)))
            {
                this.MethodFormatCSharp = this.store.GetString(CollectionPath, nameof(this.MethodFormatCSharp));
            }
            else
            {
                this.MethodFormatCSharp = Workstation.UaBrowser.Properties.Resources.MethodFormatCSharp;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.ReadOnlyValueFormatCSharp)))
            {
                this.ReadOnlyValueFormatCSharp = this.store.GetString(CollectionPath, nameof(this.ReadOnlyValueFormatCSharp));
            }
            else
            {
                this.ReadOnlyValueFormatCSharp = Workstation.UaBrowser.Properties.Resources.ReadOnlyValueFormatCSharp;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.StructureFormatCSharp)))
            {
                this.StructureFormatCSharp = this.store.GetString(CollectionPath, nameof(this.StructureFormatCSharp));
            }
            else
            {
                this.StructureFormatCSharp = Workstation.UaBrowser.Properties.Resources.StructureFormatCSharp;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.ValueFormatBasic)))
            {
                this.ValueFormatBasic = this.store.GetString(CollectionPath, nameof(this.ValueFormatBasic));
            }
            else
            {
                this.ValueFormatBasic = Workstation.UaBrowser.Properties.Resources.ValueFormatBasic;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.EventFormatBasic)))
            {
                this.EventFormatBasic = this.store.GetString(CollectionPath, nameof(this.EventFormatBasic));
            }
            else
            {
                this.EventFormatBasic = Workstation.UaBrowser.Properties.Resources.EventFormatBasic;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.MethodFormatBasic)))
            {
                this.MethodFormatBasic = this.store.GetString(CollectionPath, nameof(this.MethodFormatBasic));
            }
            else
            {
                this.MethodFormatBasic = Workstation.UaBrowser.Properties.Resources.MethodFormatBasic;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.ReadOnlyValueFormatBasic)))
            {
                this.ReadOnlyValueFormatBasic = this.store.GetString(CollectionPath, nameof(this.ReadOnlyValueFormatBasic));
            }
            else
            {
                this.ReadOnlyValueFormatBasic = Workstation.UaBrowser.Properties.Resources.ReadOnlyValueFormatBasic;
            }

            if (this.store != null && this.store.PropertyExists(CollectionPath, nameof(this.StructureFormatBasic)))
            {
                this.StructureFormatBasic = this.store.GetString(CollectionPath, nameof(this.StructureFormatBasic));
            }
            else
            {
                this.StructureFormatBasic = Workstation.UaBrowser.Properties.Resources.StructureFormatBasic;
            }
        }

        public async Task RefreshAsync(string endpointUrl, string userName, string password)
        {
            Trace.TraceInformation("Begin RefreshAsync {0}", endpointUrl);
            try
            {
                this.BrowseStop(null);
                await this.@lock.WaitAsync(this.cts.Token);
                this.NotifyPropertyChanged("IsLoading");
                try
                {
                    this.NamespaceItems.Clear();
                    if (this.channel != null)
                    {
                        try
                        {
                            await this.channel.CloseAsync(new CancellationTokenSource(2000).Token);
                        }
                        catch
                        {
                        }

                        this.channel = null;
                    }

                    this.EndpointUrl = endpointUrl;
                    this.userName = userName;
                    this.password = password;

                    if (string.IsNullOrEmpty(endpointUrl))
                    {
                        return;
                    }

                    if (this.History.Count == 0 || this.History[0] != endpointUrl)
                    {
                        this.History.Insert(0, endpointUrl);
                    }

                    while (this.History.Count > 5)
                    {
                        this.History.RemoveAt(5);
                    }

                    this.typeCache = new Dictionary<ExpandedNodeId, string>()
                    {
                        [ExpandedNodeId.Parse(DataTypeIds.Boolean)] = typeof(bool).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.SByte)] = typeof(sbyte).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Byte)] = typeof(byte).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Int16)] = typeof(short).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.UInt16)] = typeof(ushort).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Int32)] = typeof(int).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.UInt32)] = typeof(uint).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Int64)] = typeof(long).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.UInt64)] = typeof(ulong).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Float)] = typeof(float).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Double)] = typeof(double).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.String)] = typeof(string).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.DateTime)] = typeof(DateTime).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Guid)] = typeof(Guid).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.ByteString)] = typeof(byte[]).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.XmlElement)] = typeof(XElement).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.NodeId)] = typeof(NodeId).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.ExpandedNodeId)] = typeof(ExpandedNodeId).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.StatusCode)] = typeof(StatusCode).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.QualifiedName)] = typeof(QualifiedName).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.LocalizedText)] = typeof(LocalizedText).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Structure)] = typeof(ExtensionObject).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.BaseDataType)] = typeof(object).Name,
                        [ExpandedNodeId.Parse(DataTypeIds.Enumeration)] = typeof(int).Name,
                    };

                    var root = new ReferenceDescriptionViewModel(new ReferenceDescription { DisplayName = "Root", NodeId = ExpandedNodeId.Parse(ObjectIds.RootFolder), NodeClass = NodeClass.Object, TypeDefinition = ExpandedNodeId.Parse(ObjectTypeIds.FolderType) }, null, this.LoadChildrenAsync);
                    this.NamespaceItems.Add(root);
                    root.IsExpanded = true;
                    Trace.TraceInformation("Success RefreshAsync {0}", endpointUrl);
                }
                finally
                {
                    this.@lock.Release();
                    this.NotifyPropertyChanged("IsLoading");
                }
            }
            catch (OperationCanceledException)
            {
                Trace.TraceInformation("Canceled RefreshAsync {0}", endpointUrl);
            }
        }

        private void BrowseStop(object parameter)
        {
            this.cts.Cancel();
            this.cts.Dispose();
            this.cts = new CancellationTokenSource();
        }

        private void ResetSettings(object parameter)
        {
            if (this.store == null)
            {
                return;
            }

            if (this.store.CollectionExists(CollectionPath))
            {
                this.store.DeleteCollection(CollectionPath);
            }

            this.LoadSettings();
            this.LoadHistory();
        }

        private void SaveHistory()
        {
            if (this.store == null)
            {
                return;
            }

            if (!this.store.CollectionExists(CollectionPath))
            {
                this.store.CreateCollection(CollectionPath);
            }

            this.store.SetString(CollectionPath, nameof(this.History), string.Join("|", this.History));
        }

        private void SaveSettings(object parameter)
        {
            if (this.store == null)
            {
                return;
            }

            if (!this.store.CollectionExists(CollectionPath))
            {
                this.store.CreateCollection(CollectionPath);
            }

            this.store.SetString(CollectionPath, nameof(this.ValueFormatCSharp), this.ValueFormatCSharp);
            this.store.SetString(CollectionPath, nameof(this.EventFormatCSharp), this.EventFormatCSharp);
            this.store.SetString(CollectionPath, nameof(this.MethodFormatCSharp), this.MethodFormatCSharp);
            this.store.SetString(CollectionPath, nameof(this.ReadOnlyValueFormatCSharp), this.ReadOnlyValueFormatCSharp);
            this.store.SetString(CollectionPath, nameof(this.StructureFormatCSharp), this.StructureFormatCSharp);
            this.store.SetString(CollectionPath, nameof(this.ValueFormatBasic), this.ValueFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.EventFormatBasic), this.EventFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.MethodFormatBasic), this.MethodFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.ReadOnlyValueFormatBasic), this.ReadOnlyValueFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.StructureFormatBasic), this.StructureFormatBasic);
        }
    }
}