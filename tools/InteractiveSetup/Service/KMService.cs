// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup.Service;

internal static class KMService
{
    public static void Setup(Context ctx)
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "How should Kernel Memory service run and handle memory and documents ingestion?",
            Description = "KM provides a HTTP web service for uploading documents, searching, asking questions, etc. The " +
                          "service can be configured to run ingestion (loading documents) asynchronously or synchronously. " +
                          "When running asynchronously, handlers run in the background and use distributed queues to enable " +
                          "long running tasks, to retry in case of errors, and to allow scaling the service horizontally. " +
                          "The web service can also be disabled in case the queued jobs are populated differently.",
            Options =
            [
                new("Web Service with Asynchronous Ingestion Handlers (better for retry logic and long operations)",
                    config.Service.RunWebService && config.Service.RunHandlers,
                    () =>
                    {
                        ctx.CfgWebService.Value = true;
                        ctx.CfgQueue.Value = true;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = true;
                            x.Service.RunHandlers = true;
                            x.DataIngestion.OrchestrationType = KernelMemoryConfig.OrchestrationTypeDistributed;
                        });
                    }),

                new("Web Service with Synchronous Ingestion Handlers",
                    config.Service.RunWebService && !config.Service.RunHandlers,
                    () =>
                    {
                        ctx.CfgWebService.Value = true;
                        ctx.CfgQueue.Value = false;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = true;
                            x.Service.RunHandlers = false;
                            x.DataIngestion.OrchestrationType = KernelMemoryConfig.OrchestrationTypeInProcess;
                            x.DataIngestion.DistributedOrchestration.QueueType = "";
                        });
                    }),

                new("No web Service, run only asynchronous Ingestion Handlers in the background",
                    !config.Service.RunWebService && config.Service.RunHandlers,
                    () =>
                    {
                        ctx.CfgWebService.Value = false;
                        ctx.CfgQueue.Value = true;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = false;
                            x.Service.RunHandlers = true;
                            x.DataIngestion.OrchestrationType = KernelMemoryConfig.OrchestrationTypeDistributed;
                        });
                    }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }
}
