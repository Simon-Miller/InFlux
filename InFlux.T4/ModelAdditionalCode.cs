
/* GENERATED CODE!  DON'T EDIT IT, OR YOU WILL LIKELY LOSE CHANGES IN FUTURE!
   LAST GENERATED: 06/28/2023 15:31:34
*/

using System;
using System.Diagnostics;
using InFlux.Attributes;
using InFlux;
using System.ComponentModel.DataAnnotations;

namespace InFlux.T4.TestModels
{
    public partial class TestClass : IAutoWireup
    {
        public TestClass(IntentProcessor intentProcessor)
        {
            IntentProcessor = intentProcessor;

            var factory = new InsightsFactory(IntentProcessor);

            var idResources = factory.Make(id);
            IdInsights = idResources.insight;
            IdInsightsManager = idResources.manager;
            IdInsights.OnValueChanged.Subscribe((O, N) => OnEntityChanged.FireEvent());

            var nameResources = factory.Make(name);
            NameInsights = nameResources.insight;
            NameInsightsManager = nameResources.manager;
            NameInsights.OnValueChanged.Subscribe((O, N) => OnEntityChanged.FireEvent());

        }

        public readonly IntentProcessor IntentProcessor;

        public void ResetToPristine()
        {
            IdInsights.ResetToPristine();
            NameInsights.ResetToPristine();
        }

        public bool ModelTouched => IdInsights.IsTouched || NameInsights.IsTouched;

        public bool ModelDirty => IdInsights.IsDirty || NameInsights.IsDirty;

        public QueuedEvent OnEntityChanged { get; } = new QueuedEvent();

        [Required]
        public int Id => id;
        public readonly Insights<int> IdInsights;
        private readonly IOwnInsight IdInsightsManager;

        [DebuggerStepThrough]
        public void TrySetId(int newValue, Action? codeIfAllowed = null, Action? codeIfNotAllowed = null) =>
            IntentHelper.TrySet<int>(IntentProcessor, "TestClass", "Id", () => id, x => id = x, newValue, 
                                            IdInsights, IdInsightsManager, codeIfAllowed, codeIfNotAllowed);
        public string Name => name;
        public readonly Insights<string> NameInsights;
        private readonly IOwnInsight NameInsightsManager;

        [DebuggerStepThrough]
        public void TrySetName(string newValue, Action? codeIfAllowed = null, Action? codeIfNotAllowed = null) =>
            IntentHelper.TrySet<string>(IntentProcessor, "TestClass", "Name", () => name, x => name = x, newValue, 
                                            NameInsights, NameInsightsManager, codeIfAllowed, codeIfNotAllowed);
    }
}

// end of generated code.