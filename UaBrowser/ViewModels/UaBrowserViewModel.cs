// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.UaBrowser.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EnvDTE;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell.Settings;
    using Workstation.ServiceModel.Ua;
    using Workstation.ServiceModel.Ua.Channels;

    [Export]
    public class UaBrowserViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string CollectionPath = "UaBrowser";
        private const string EventDataTypeCS = "BaseEvent";
        private const string EventDataTypeVB = "BaseEvent";
        private const string MethodDataTypeCS = "Task<object[]>";
        private const string MethodDataTypeVB = "Task(Of Object())";
        private static readonly Regex SafeCharsRegex = new Regex(@"[\W]");

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
        private UaTcpSessionClient session;
        private WritableSettingsStore store;
        private string valueFormatBasic;
        private string valueFormatCSharp;
        private string methodFormatBasic;
        private string methodFormatCSharp;
        private ApplicationDescription localDescription;
        private X509Certificate2 localCertificate;

        public UaBrowserViewModel()
        {
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

            this.RefreshCommand = new DelegateCommand(async (s) => await this.RefreshAsync(s));
            this.BrowseStopCommand = new DelegateCommand(this.BrowseStop);
            this.SaveSettingsCommand = new DelegateCommand(this.SaveSettings);
            this.ResetSettingsCommand = new DelegateCommand(this.ResetSettings);
            this.NamespaceItems = new ObservableCollection<ReferenceDescriptionViewModel>();
            this.ide = (EnvDTE.DTE)vsServiceProvider.GetService(typeof(EnvDTE.DTE));
            var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
            this.store = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            this.LoadSettings();
            this.LoadHistory();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand BrowseStopCommand { get; private set; }

        public string EndpointUrl
        {
            get
            {
                return this.endpointUrl;
            }

            private set
            {
                this.endpointUrl = value;
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

        public ICommand RefreshCommand { get; private set; }

        public ICommand ResetSettingsCommand { get; private set; }

        public ICommand SaveSettingsCommand { get; private set; }

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

        public void Dispose()
        {
            this.SaveHistory();
            if (this.session != null)
            {
                try
                {
                    this.session.CloseAsync().Wait();
                }
                catch (Exception)
                {
                    this.session.AbortAsync().Wait();
                }
            }

            if (this.cts != null)
            {
                this.cts.Cancel();
                this.cts.Dispose();
                this.cts = null;
            }
        }

        public string FormatProperty(ReferenceDescriptionViewModel vm)
        {
            var fcm = this.ide?.ActiveDocument?.ProjectItem?.FileCodeModel;
            var language = fcm?.Language ?? CodeModelLanguageConstants.vsCMLanguageCSharp;

            switch (language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    if (vm.IsVariable)
                    {
                        if (vm.AccessLevel.HasFlag(AccessLevelFlags.CurrentWrite))
                        {
                            return FormatSnippet(this.ValueFormatCSharp, vm.DisplayName, vm.FullName, FormatTypeName(vm.DataType, language), vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                        }

                        return FormatSnippet(this.ReadOnlyValueFormatCSharp, vm.DisplayName, vm.FullName, FormatTypeName(vm.DataType, language), vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    if (vm.IsMethod)
                    {
                        return FormatSnippet(this.MethodFormatCSharp, vm.DisplayName, vm.FullName, MethodDataTypeCS, vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    if (vm.IsEventNotifier)
                    {
                        return FormatSnippet(this.EventFormatCSharp, vm.DisplayName, vm.FullName, EventDataTypeCS, vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    break;

                case CodeModelLanguageConstants.vsCMLanguageVB:
                    if (vm.IsVariable)
                    {
                        if (vm.AccessLevel.HasFlag(AccessLevelFlags.CurrentWrite))
                        {
                            return FormatSnippet(this.ValueFormatBasic, vm.DisplayName, vm.FullName, FormatTypeName(vm.DataType, language), vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                        }

                        return FormatSnippet(this.ReadOnlyValueFormatBasic, vm.DisplayName, vm.FullName, FormatTypeName(vm.DataType, language), vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    if (vm.IsMethod)
                    {
                        return FormatSnippet(this.MethodFormatBasic, vm.DisplayName, vm.FullName, MethodDataTypeVB, vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    if (vm.IsEventNotifier)
                    {
                        return FormatSnippet(this.EventFormatBasic, vm.DisplayName, vm.FullName, EventDataTypeVB, vm.NodeId.ToString(), vm.Parent.NodeId.ToString(), vm.BrowseName.ToString());
                    }

                    break;
            }

            return string.Empty;
        }

        public void CodeElementFromPoint()
        {
            try
            {
                TextSelection sel = this.ide?.ActiveDocument?.Selection as TextSelection;
                TextPoint pnt = (TextPoint)sel.ActivePoint;

                // Discover every code element containing the insertion point.
                FileCodeModel fcm = this.ide.ActiveDocument.ProjectItem.FileCodeModel;
                string elems = string.Empty;
                CodeElement elem = fcm.CodeElementFromPoint(pnt, vsCMElement.vsCMElementClass);

                if (elem != null)
                {
                    Trace.TraceInformation(
                        "The following element contain the insertion point:\n\n" +
                        elem.Name);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
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
                    case CodeModelLanguageConstants.vsCMLanguageCSharp:
                        return t.GetElementType().Name + "[]";

                    case CodeModelLanguageConstants.vsCMLanguageVB:
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
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    txt.Append(t.Name, 0, t.Name.IndexOf('`'));
                    txt.Append("<");
                    txt.Append(string.Join(", ", t.GetGenericArguments().Select(arg => FormatTypeName(arg, language))));
                    txt.Append(">");
                    return txt.ToString();

                case CodeModelLanguageConstants.vsCMLanguageVB:
                    txt.Append(t.Name, 0, t.Name.IndexOf('`'));
                    txt.Append("(Of ");
                    txt.Append(string.Join(", ", t.GetGenericArguments().Select(arg => FormatTypeName(arg, language))));
                    txt.Append(")");
                    return txt.ToString();
            }

            return t.Name;
        }

        private static string FormatSnippet(string snippet, string name, string fullName, string datatype, string nodeId, string parentNodeId, string browseName)
        {
            var s = new StringBuilder(snippet);
            s.Replace("$name$", name);
            s.Replace("$propertyName$", ToPropertyName(fullName));
            s.Replace("$fieldName$", ToFieldName(fullName));
            s.Replace("$dataType$", datatype);
            s.Replace("$nodeId$", nodeId);
            s.Replace("$parentNodeId$", parentNodeId);
            s.Replace("$browseName$", browseName);
            s.AppendLine();
            return s.ToString();
        }

        private static string ToFieldName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "x";
            }

            text = SafeCharsRegex.Replace(text, "_");
            var charArray = text.ToCharArray();
            if (!char.IsLetter(charArray[0]))
            {
                return "x" + text;
            }

            charArray[0] = char.ToLowerInvariant(charArray[0]);
            return new string(charArray);
        }

        private static string ToPropertyName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "X";
            }

            text = SafeCharsRegex.Replace(text, "_");
            var charArray = text.ToCharArray();
            if (!char.IsLetter(charArray[0]))
            {
                return "X" + text;
            }

            charArray[0] = char.ToUpperInvariant(charArray[0]);
            return new string(charArray);
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
                        try
                        {
                            if (this.session == null || this.session.State != CommunicationState.Opened)
                            {
                                var getEndpointsRequest = new GetEndpointsRequest
                                {
                                    EndpointUrl = this.endpointUrl,
                                    ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                                };
                                var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);
                                token.ThrowIfCancellationRequested();
                                var selectedEndpoint = getEndpointsResponse.Endpoints.OrderByDescending(e => e.SecurityLevel).Last();
                                if (this.localCertificate == null)
                                {
                                    this.localCertificate = this.localDescription.GetCertificate(createIfNotFound: true);
                                }

                                this.session = new UaTcpSessionClient(
                                    this.localDescription,
                                    this.localCertificate,
                                    null,
                                    selectedEndpoint);
                                await this.session.OpenAsync();
                            }

                            token.ThrowIfCancellationRequested();
                            var browseRequest = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(parent.NodeId, this.session.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HierarchicalReferences), ResultMask = (uint)BrowseResultMask.All, NodeClassMask = (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = true } }, RequestedMaxReferencesPerNode = 64 };
                            var browseResponse = await this.session.BrowseAsync(browseRequest);
                            var refs = browseResponse.Results.Where(result => result.References != null).SelectMany(result => result.References).ToArray();
                            if (refs.Length == 0)
                            {
                                return;
                            }

                            var nodes = refs.Select(r => ExpandedNodeId.ToNodeId(r.NodeId, this.session.NamespaceUris)).ToArray();
                            var readTypeRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.DataType }).ToArray() };
                            var readTypeResponse = await this.session.ReadAsync(readTypeRequest);
                            var readRankRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.ValueRank }).ToArray() };
                            var readRankResponse = await this.session.ReadAsync(readRankRequest);
                            var readNotifierRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.EventNotifier }).ToArray() };
                            var readNotifierResponse = await this.session.ReadAsync(readNotifierRequest);
                            var readAccessLevelRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.UserAccessLevel }).ToArray() };
                            var readAccessLevelResponse = await this.session.ReadAsync(readAccessLevelRequest);

                            NodeId dataTypeNode;
                            Type dataType;
                            int valueRank;
                            EventNotifierFlags notifier;
                            AccessLevelFlags accessLevel;
                            ExpandedNodeId dataTypeId;
                            ReferenceDescription dataTypeRef;
                            for (int i = 0; i < refs.Length; i++)
                            {
                                dataTypeNode = readTypeResponse.Results[i].GetValueOrDefault(NodeId.Null);
                                if (dataTypeNode != NodeId.Null)
                                {
                                    dataTypeId = NodeId.ToExpandedNodeId(dataTypeNode, this.session.NamespaceUris);
                                    if (!UaTcpSecureChannel.DataTypeIdToTypeDictionary.TryGetValue(dataTypeId, out dataType))
                                    {
                                        do
                                        {
                                            dataTypeNode = ExpandedNodeId.ToNodeId(dataTypeId, this.session.NamespaceUris);
                                            var browseRequest2 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = dataTypeNode, ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasSubtype), ResultMask = (uint)BrowseResultMask.None, NodeClassMask = (uint)NodeClass.DataType, BrowseDirection = BrowseDirection.Inverse, IncludeSubtypes = false } } };
                                            var browseResponse2 = await this.session.BrowseAsync(browseRequest2);
                                            dataTypeRef = browseResponse2.Results[0].References?.FirstOrDefault();
                                            dataTypeId = dataTypeRef?.NodeId;
                                        }
                                        while (dataTypeId != null && !UaTcpSecureChannel.DataTypeIdToTypeDictionary.TryGetValue(dataTypeId, out dataType));

                                        if (dataTypeId == null)
                                        {
                                            dataType = typeof(object);
                                        }
                                    }

                                    valueRank = readRankResponse.Results[i].GetValueOrDefault(-1);
                                    if (valueRank == 1)
                                    {
                                        dataType = dataType.MakeArrayType();
                                    }

                                    if (valueRank > 1)
                                    {
                                        dataType = dataType.MakeArrayType(valueRank);
                                    }
                                }
                                else
                                {
                                    dataType = typeof(object);
                                }

                                notifier = (EventNotifierFlags)Enum.ToObject(typeof(EventNotifierFlags), readNotifierResponse.Results[i].GetValueOrDefault<byte>());
                                accessLevel = (AccessLevelFlags)Enum.ToObject(typeof(AccessLevelFlags), readAccessLevelResponse.Results[i].GetValueOrDefault<byte>());
                                parent.Children.Add(new ReferenceDescriptionViewModel(refs[i], dataType, accessLevel, notifier, parent, this.LoadChildrenAsync));
                                await Task.Yield();
                            }

                            var continuationPoints = browseResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
                            while (continuationPoints.Length > 0)
                            {
                                token.ThrowIfCancellationRequested();
                                var browseNextRequest = new BrowseNextRequest { ContinuationPoints = continuationPoints, ReleaseContinuationPoints = false };
                                var browseNextResponse = await this.session.BrowseNextAsync(browseNextRequest);
                                refs = browseNextResponse.Results.Where(result => result.References != null).SelectMany(result => result.References).ToArray();
                                if (refs.Length == 0)
                                {
                                    return;
                                }

                                nodes = refs.Select(r => ExpandedNodeId.ToNodeId(r.NodeId, this.session.NamespaceUris)).ToArray();
                                readTypeRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.DataType }).ToArray() };
                                readTypeResponse = await this.session.ReadAsync(readTypeRequest);
                                readRankRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.ValueRank }).ToArray() };
                                readRankResponse = await this.session.ReadAsync(readRankRequest);
                                readNotifierRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.EventNotifier }).ToArray() };
                                readNotifierResponse = await this.session.ReadAsync(readNotifierRequest);
                                readAccessLevelRequest = new ReadRequest { NodesToRead = nodes.Select(n => new ReadValueId { NodeId = n, AttributeId = AttributeIds.UserAccessLevel }).ToArray() };
                                readAccessLevelResponse = await this.session.ReadAsync(readAccessLevelRequest);
                                for (int i = 0; i < refs.Length; i++)
                                {
                                    dataTypeNode = readTypeResponse.Results[i].GetValueOrDefault(NodeId.Null);
                                    if (dataTypeNode != NodeId.Null)
                                    {
                                        dataTypeId = NodeId.ToExpandedNodeId(dataTypeNode, this.session.NamespaceUris);
                                        if (!UaTcpSecureChannel.DataTypeIdToTypeDictionary.TryGetValue(dataTypeId, out dataType))
                                        {
                                            do
                                            {
                                                dataTypeNode = ExpandedNodeId.ToNodeId(dataTypeId, this.session.NamespaceUris);
                                                var browseRequest2 = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = dataTypeNode, ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HasSubtype), ResultMask = (uint)BrowseResultMask.None, NodeClassMask = (uint)NodeClass.DataType, BrowseDirection = BrowseDirection.Inverse, IncludeSubtypes = false } } };
                                                var browseResponse2 = await this.session.BrowseAsync(browseRequest2);
                                                dataTypeRef = browseResponse2.Results[0].References?.FirstOrDefault();
                                                dataTypeId = dataTypeRef?.NodeId;
                                            }
                                            while (dataTypeId != null && !UaTcpSecureChannel.DataTypeIdToTypeDictionary.TryGetValue(dataTypeId, out dataType));

                                            if (dataTypeId == null)
                                            {
                                                dataType = typeof(object);
                                            }
                                        }

                                        valueRank = readRankResponse.Results[i].GetValueOrDefault(-1);
                                        if (valueRank == 1)
                                        {
                                            dataType = dataType.MakeArrayType();
                                        }

                                        if (valueRank > 1)
                                        {
                                            dataType = dataType.MakeArrayType(valueRank);
                                        }
                                    }
                                    else
                                    {
                                        dataType = typeof(object);
                                    }

                                    notifier = (EventNotifierFlags)Enum.ToObject(typeof(EventNotifierFlags), readNotifierResponse.Results[i].GetValueOrDefault<byte>());
                                    accessLevel = (AccessLevelFlags)Enum.ToObject(typeof(AccessLevelFlags), readAccessLevelResponse.Results[i].GetValueOrDefault<byte>());
                                    parent.Children.Add(new ReferenceDescriptionViewModel(refs[i], dataType, accessLevel, notifier, parent, this.LoadChildrenAsync));
                                    await Task.Yield();
                                }

                                continuationPoints = browseNextResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
                            }

                            break;
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (ServiceResultException ex)
                        {
                            Trace.TraceInformation("ServiceResultException: {0}", ex);
                            if (this.session != null)
                            {
                                await this.session.AbortAsync(token);
                                this.session = null;
                            }

                            if (ex.HResult == unchecked((int)StatusCodes.BadSessionIdInvalid))
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation("Exception {0}", ex);
                            if (this.session != null)
                            {
                                await this.session.AbortAsync(token);
                                this.session = null;
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
        }

        private async Task RefreshAsync(object parameter)
        {
            var endpointUrl = parameter as string;
            Trace.TraceInformation("BrowseAsync {0}", endpointUrl);
            try
            {
                this.BrowseStop(null);
                await this.@lock.WaitAsync(this.cts.Token);
                this.NotifyPropertyChanged("IsLoading");
                try
                {
                    this.NamespaceItems.Clear();
                    if (this.session != null)
                    {
                        try
                        {
                            await this.session.CloseAsync();
                        }
                        catch (Exception)
                        {
                        }

                        this.session = null;
                    }

                    this.EndpointUrl = endpointUrl;
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

                    var root = new ReferenceDescriptionViewModel(new ReferenceDescription { DisplayName = "Objects", NodeId = ExpandedNodeId.Parse(ObjectIds.ObjectsFolder), NodeClass = NodeClass.Object, TypeDefinition = ExpandedNodeId.Parse(ObjectTypeIds.FolderType) }, null, AccessLevelFlags.None, EventNotifierFlags.None, null, this.LoadChildrenAsync);
                    this.NamespaceItems.Add(root);
                    root.IsExpanded = true;
                }
                finally
                {
                    this.@lock.Release();
                    this.NotifyPropertyChanged("IsLoading");
                }
            }
            catch (OperationCanceledException)
            {
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
            this.store.SetString(CollectionPath, nameof(this.ValueFormatBasic), this.ValueFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.EventFormatBasic), this.EventFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.MethodFormatBasic), this.MethodFormatBasic);
            this.store.SetString(CollectionPath, nameof(this.ReadOnlyValueFormatBasic), this.ReadOnlyValueFormatBasic);
        }
    }
}