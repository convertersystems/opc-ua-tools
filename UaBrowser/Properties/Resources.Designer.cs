﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Workstation.UaBrowser.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Workstation.UaBrowser.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to     &apos;&apos;&apos; &lt;summary&gt;
        ///    &apos;&apos;&apos; Gets the event of $propertyName$.
        ///    &apos;&apos;&apos; &lt;/summary&gt;
        ///    &lt;MonitoredItem(nodeId: &quot;$nodeId$&quot;, attributeId: AttributeIds.EventNotifier)&gt;
        ///    Public Property $propertyName$() As $dataType$
        ///    Get
        ///        Return _$fieldName$
        ///    End Get
        ///    Private Set(ByVal value As $dataType$)
        ///        SetProperty(_$fieldName$, value)
        ///    End Set
        ///    End Property
        ///
        ///    Private _$fieldName$ As $dataType$
        ///.
        /// </summary>
        internal static string EventFormatBasic {
            get {
                return ResourceManager.GetString("EventFormatBasic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to         /// &lt;summary&gt;
        ///        /// Gets the event of $propertyName$.
        ///        /// &lt;/summary&gt;
        ///        [MonitoredItem(nodeId: &quot;$nodeId$&quot;, attributeId: AttributeIds.EventNotifier)]
        ///        public $dataType$ $propertyName$
        ///        {
        ///            get { return this.$fieldName$; }
        ///            private set { this.SetProperty(ref this.$fieldName$, value); }
        ///        }
        ///
        ///        private $dataType$ $fieldName$;
        ///.
        /// </summary>
        internal static string EventFormatCSharp {
            get {
                return ResourceManager.GetString("EventFormatCSharp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to     &apos;&apos;&apos; &lt;summary&gt;
        ///    &apos;&apos;&apos; Invokes the method $propertyName$
        ///    &apos;&apos;&apos; &lt;/summary&gt;
        ///    Public Async Function $propertyName$(ByVal ParamArray inArgs() As Object) As Task(Of Object())
        ///
        ///        Dim response As CallResponse = Await Session.CallAsync(New CallRequest With
        ///            {
        ///               .MethodsToCall =
        ///                {
        ///                    New CallMethodRequest With
        ///                    {
        ///                        .ObjectId = NodeId.Parse(&quot;$parentNodeId$&quot;),
        ///                        .MethodId =  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string MethodFormatBasic {
            get {
                return ResourceManager.GetString("MethodFormatBasic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to         /// &lt;summary&gt;
        ///        /// Invokes the method $propertyName$.
        ///        /// &lt;/summary&gt;
        ///        public async Task&lt;object[]&gt; $propertyName$(params object[] inArgs)
        ///        {
        ///            var response = await this.Session.CallAsync(new CallRequest
        ///            {
        ///               MethodsToCall = new[]
        ///                {
        ///                    new CallMethodRequest
        ///                    {
        ///                        ObjectId = NodeId.Parse(&quot;$parentNodeId$&quot;),
        ///                        MethodId = NodeId.Parse(&quot;$n [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string MethodFormatCSharp {
            get {
                return ResourceManager.GetString("MethodFormatCSharp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to     &apos;&apos;&apos; &lt;summary&gt;
        ///    &apos;&apos;&apos; Gets the value of $propertyName$.
        ///    &apos;&apos;&apos; &lt;/summary&gt;
        ///    &lt;MonitoredItem(nodeId: &quot;$nodeId$&quot;)&gt;
        ///    Public Property $propertyName$() As $dataType$
        ///    Get
        ///        Return _$fieldName$
        ///    End Get
        ///    Private Set(ByVal value As $dataType$)
        ///        SetProperty(_$fieldName$, value)
        ///    End Set
        ///    End Property
        ///
        ///    Private _$fieldName$ As $dataType$
        ///.
        /// </summary>
        internal static string ReadOnlyValueFormatBasic {
            get {
                return ResourceManager.GetString("ReadOnlyValueFormatBasic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to         /// &lt;summary&gt;
        ///        /// Gets the value of $propertyName$.
        ///        /// &lt;/summary&gt;
        ///        [MonitoredItem(nodeId: &quot;$nodeId$&quot;)]
        ///        public $dataType$ $propertyName$
        ///        {
        ///            get { return this.$fieldName$; }
        ///            private set { this.SetProperty(ref this.$fieldName$, value); }
        ///        }
        ///
        ///        private $dataType$ $fieldName$;.
        /// </summary>
        internal static string ReadOnlyValueFormatCSharp {
            get {
                return ResourceManager.GetString("ReadOnlyValueFormatCSharp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to     &apos;&apos;&apos; &lt;summary&gt;
        ///    &apos;&apos;&apos; Gets or sets the value of $propertyName$.
        ///    &apos;&apos;&apos; &lt;/summary&gt;
        ///    &lt;MonitoredItem(nodeId: &quot;$nodeId$&quot;)&gt;
        ///    Public Property $propertyName$() As $dataType$
        ///    Get
        ///        Return _$fieldName$
        ///    End Get
        ///    Set(ByVal value As $dataType$)
        ///        SetProperty(_$fieldName$, value)
        ///    End Set
        ///    End Property
        ///
        ///    Private _$fieldName$ As $dataType$
        ///.
        /// </summary>
        internal static string ValueFormatBasic {
            get {
                return ResourceManager.GetString("ValueFormatBasic", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to         /// &lt;summary&gt;
        ///        /// Gets or sets the value of $propertyName$.
        ///        /// &lt;/summary&gt;
        ///        [MonitoredItem(nodeId: &quot;$nodeId$&quot;)]
        ///        public $dataType$ $propertyName$
        ///        {
        ///            get { return this.$fieldName$; }
        ///            set { this.SetProperty(ref this.$fieldName$, value); }
        ///        }
        ///
        ///        private $dataType$ $fieldName$;
        ///.
        /// </summary>
        internal static string ValueFormatCSharp {
            get {
                return ResourceManager.GetString("ValueFormatCSharp", resourceCulture);
            }
        }
    }
}