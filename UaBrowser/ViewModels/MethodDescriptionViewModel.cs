// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Workstation.ServiceModel.Ua;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Workstation.UaBrowser.ViewModels
{
    public class MethodDescriptionViewModel : ReferenceDescriptionViewModel
    {
        private readonly Parameter[] inArgs;
        private readonly Parameter[] outArgs;

        public MethodDescriptionViewModel(ReferenceDescription description, Parameter[] inArgs, Parameter[] outArgs, ReferenceDescriptionViewModel parent, Func<ReferenceDescriptionViewModel, Task> loadChildren)
            : base(description, parent, loadChildren)
        {
            this.inArgs = inArgs;
            this.outArgs = outArgs;
        }

        public string ReturnType => this.GetReturnTypeCS();

        public string Parameters => this.GetParametersCS();

        public override string GetSnippet(string snippet, string language)
        {
            var s = new StringBuilder(snippet);

            switch (language)
            {
                case UaBrowserViewModel.VsCMLanguageCSharp:
                    s.Replace("$name$", this.DisplayName);
                    s.Replace("$browseName$", this.BrowseName.ToString());
                    s.Replace("$fullName$", this.FullName);
                    s.Replace("$returnType$", this.GetReturnTypeCS());
                    s.Replace("$returnValue$", this.GetReturnValueCS());
                    s.Replace("$inputArguments$", this.GetInputArgumentsCS());
                    s.Replace("$parameters$", this.GetParametersCS());
                    s.Replace("$nodeId$", this.NodeId.ToString());
                    s.Replace("$parentNodeId$", this.Parent?.NodeId.ToString() ?? string.Empty);
                    s.AppendLine();
                    break;

                case UaBrowserViewModel.VsCMLanguageVB:
                    s.Replace("$name$", this.DisplayName);
                    s.Replace("$browseName$", this.BrowseName.ToString());
                    s.Replace("$fullName$", this.FullName);
                    s.Replace("$returnType$", this.GetReturnTypeVB());
                    s.Replace("$returnValue$", this.GetReturnValueVB());
                    s.Replace("$inputArguments$", this.GetInputArgumentsVB());
                    s.Replace("$parameters$", this.GetParametersVB());
                    s.Replace("$nodeId$", this.NodeId.ToString());
                    s.Replace("$parentNodeId$", this.Parent?.NodeId.ToString() ?? string.Empty);
                    s.AppendLine();
                    break;
            }

            return s.ToString();
        }

        private string GetReturnTypeCS()
        {
            if (this.outArgs == null || this.outArgs.Length == 0)
            {
                return "Task";
            }

            if (this.outArgs.Length == 1)
            {
                return $"Task<{this.outArgs[0].ParameterType}>";
            }

            return $"Task<({string.Join(", ", this.outArgs.Select(a => $"{a.ParameterType} {a.Name}"))})>";
        }

        private string GetReturnTypeVB()
        {
            if (this.outArgs == null || this.outArgs.Length == 0)
            {
                return "Task";
            }

            if (this.outArgs.Length == 1)
            {
                return $"Task(Of {this.outArgs[0].ParameterType.Replace('[', '(').Replace(']', ')')})";
            }

            return $"Task(Of ({string.Join(", ", this.outArgs.Select(a => $"{a.Name} As {a.ParameterType.Replace('[', '(').Replace(']', ')')}"))}))";
        }

        private string GetReturnValueCS()
        {
            if (this.outArgs == null || this.outArgs.Length == 0)
            {
                return string.Empty;
            }

            if (this.outArgs.Length == 1)
            {
                return $"result.OutputArguments[0].GetValueOrDefault<{this.outArgs[0].ParameterType}>()";
            }

            return $"({string.Join(", ", this.outArgs.Select((a, i) => $"result.OutputArguments[{i}].GetValueOrDefault<{a.ParameterType}>()"))})";
        }

        private string GetReturnValueVB()
        {
            if (this.outArgs == null || this.outArgs.Length == 0)
            {
                return string.Empty;
            }

            if (this.outArgs.Length == 1)
            {
                return $"result.OutputArguments(0).GetValueOrDefault(Of {this.outArgs[0].ParameterType.Replace('[', '(').Replace(']', ')')})()";
            }

            return $"({string.Join(", ", this.outArgs.Select((a, i) => $"result.OutputArguments({i}).GetValueOrDefault(Of {a.ParameterType.Replace('[', '(').Replace(']', ')')})()"))})";
        }

        private string GetParametersCS()
        {
            return (this.inArgs != null && this.inArgs.Length > 0) ? string.Join(", ", this.inArgs.Select(a => $"{a.ParameterType} {a.Name}")) : string.Empty;
        }

        private string GetParametersVB()
        {
            return (this.inArgs != null && this.inArgs.Length > 0) ? string.Join(", ", this.inArgs.Select(a => $"ByVal {a.Name} As {a.ParameterType.Replace('[', '(').Replace(']', ')')}")) : string.Empty;
        }

        private string GetInputArgumentsCS()
        {
            return (this.inArgs != null && this.inArgs.Length > 0) ? $"new Variant[] {{ {string.Join(", ", this.inArgs.Select(a => a.Name))} }}" : "new Variant[0]";
        }

        private string GetInputArgumentsVB()
        {
            return (this.inArgs != null && this.inArgs.Length > 0) ? $"New [Variant]() {{ {string.Join(", ", this.inArgs.Select(a => a.Name))} }}" : "New [Variant](0)";
        }
    }
}

/*
       /// <summary>
       /// Invokes the method ServerGetMonitoredItems.
       /// </summary>
       /// <param name="SubscriptionId">The SubscriptionId.</param>
       /// <returns>A <see cref="Task"/> that returns the output arguments.</returns>
       public async Task<(uint[] ServerHandles, uint[] ClientHandles)> ServerGetMonitoredItems(uint SubscriptionId)
       {
           var response = await this.InnerChannel.CallAsync(new CallRequest
           {
               MethodsToCall = new[]
               {
                   new CallMethodRequest
                   {
                       ObjectId = NodeId.Parse("i=2253"),
                       MethodId = NodeId.Parse("i=11492"),
                       InputArguments = new Variant[] { SubscriptionId }
                   }
               }
           });

           var result = response.Results[0];
           if (StatusCode.IsBad(result.StatusCode))
           {
               throw new ServiceResultException(new ServiceResult(result.StatusCode));
           }

           return (result.OutputArguments[0].GetValueOrDefault<uint[]>(), result.OutputArguments[1].GetValueOrDefault<uint[]>());
       }
*/
