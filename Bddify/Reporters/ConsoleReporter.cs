﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bddify.Core;

namespace Bddify.Reporters
{
    public class ConsoleReporter : IProcessor
    {
        readonly List<Exception> _exceptions = new List<Exception>();
        private int _longestStepSentence;

        public ProcessType ProcessType
        {
            get { return ProcessType.Report; }
        }

        public void Process(Story story)
        {
            var reporterRegistry
                = new Dictionary<StepExecutionResult, Action<ExecutionStep>>
                          {
                              {StepExecutionResult.Passed, s => ReportOnStep(s)},
                              {StepExecutionResult.Failed, s => ReportOnStep(s, true)},
                              {StepExecutionResult.Inconclusive, s => ReportOnStep(s)},
                              {StepExecutionResult.NotImplemented, s => ReportOnStep(s, true)},
                              {StepExecutionResult.NotExecuted, s => ReportOnStep(s)}
                          };

            ReportStoryHeader(story);

            var allSteps = story.Scenarios.SelectMany(s => s.Steps);
            if (allSteps.Any())
                _longestStepSentence = allSteps.Max(s => s.ReadableMethodName.Length);

            foreach (var scenario in story.Scenarios)
            {
                Report(scenario);

                if (scenario.Steps.Any())
                {
                    foreach (var step in scenario.Steps.Where(s => s.ShouldReport))
                        reporterRegistry[step.Result](step);
                }
            }

            ReportExceptions();

            ReportStoryFooter(story);
        }

        private static void ReportStoryFooter(Story story)
        {
        }

        private static void ReportStoryHeader(Story story)
        {
            if (story.MetaData == null || story.MetaData.Type == null)
                return;

            Console.WriteLine("Story: " + story.MetaData.Title);
            Console.WriteLine("\t" + story.MetaData.AsA);
            Console.WriteLine("\t" + story.MetaData.IWant);
            Console.WriteLine("\t" + story.MetaData.SoThat);
        }

#if NET35
        void ReportOnStep(ExecutionStep step)
        {
            ReportOnStep(step, false);
        }
#endif

#if !NET35
        void ReportOnStep(ExecutionStep step, bool reportOnException = false)
#else
        void ReportOnStep(ExecutionStep step, bool reportOnException)
#endif
        {
            var message =
                string.Format
                    ("\t{0}  [{1}] ",
                    step.ReadableMethodName.PadRight(_longestStepSentence + 5),
                    NetToString.Convert(step.Result.ToString()));

            if(reportOnException)
            {
                _exceptions.Add(step.Exception);

                var exceptionReference = string.Format("[Details at {0} below]", _exceptions.Count);
                if (!string.IsNullOrEmpty(step.Exception.Message))
                    message += string.Format("[{0}] {1}", FlattenExceptionMessage(step.Exception.Message), exceptionReference);
                else
                    message += string.Format("{0}", exceptionReference);
            }

            if (step.Result == StepExecutionResult.Inconclusive || step.Result == StepExecutionResult.NotImplemented)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (step.Result == StepExecutionResult.Failed)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (step.Result == StepExecutionResult.NotExecuted)
                Console.ForegroundColor = ConsoleColor.Gray;
            
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        void ReportExceptions()
        {
            Console.WriteLine();
            if (_exceptions.Count == 0)
                return;

            Console.Write("Exceptions:");

            for (int index = 0; index < _exceptions.Count; index++)
            {
                var exception = _exceptions[index];
                Console.WriteLine();
                Console.Write(string.Format("  {0}. ", index + 1));
                
                if (!string.IsNullOrEmpty(exception.Message))
                    Console.WriteLine(FlattenExceptionMessage(exception.Message));
                else
                    Console.WriteLine();
                    
                Console.WriteLine(exception.StackTrace);
            }

            Console.WriteLine();
        }

        static string FlattenExceptionMessage(string message)
        {
            // ToDo: if gets complex will change it with a regex
            return message
                .Replace("\t", " ") // replace tab with one space
                .Replace(Environment.NewLine, ", ") // replace new line with one space
                .Trim() // trim starting and trailing spaces
                .Replace("  ", " ")
                .TrimEnd(','); // chop any , from the end
        }

        static void Report(Scenario scenario)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Scenario: " + scenario.ScenarioText);
        }
    }
}