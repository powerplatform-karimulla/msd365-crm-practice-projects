using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace StudentPlugin
{
    public class StudentAutoFieldsPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                Entity target = (Entity)context.InputParameters["Target"];

                try
                {
                    // =============================================
                    // REQUIREMENT 1: Auto-generate Hall Ticket No
                    // Field: shaik_hallticetno (Single Line of Text)
                    // =============================================
                    if (!target.Contains("shaik_hallticetno") ||
                        string.IsNullOrEmpty(target.GetAttributeValue<string>("shaik_hallticetno")))
                    {
                        string hallTicketNo = GenerateHallTicketNo(service, tracingService);
                        target["shaik_hallticetno"] = hallTicketNo;
                        tracingService.Trace($"Hall Ticket Generated: {hallTicketNo}");
                    }

                    // =============================================
                    // REQUIREMENT 2: Calculate Total Secured Marks
                    // & Percentage
                    // =============================================
                    int telugu = GetMarkValue(target, "cr675_telugumarks");
                    int hindi = GetMarkValue(target, "cr675_hindhimarks");
                    int english = GetMarkValue(target, "cr675_englishmarks");
                    int maths = GetMarkValue(target, "cr675_mathsmarks");
                    int science = GetMarkValue(target, "cr675_sciencemarks");
                    int social = GetMarkValue(target, "cr675_socialmarks");

                    tracingService.Trace(
                        $"Marks => Telugu:{telugu}, Hindi:{hindi}, English:{english}, " +
                        $"Maths:{maths}, Science:{science}, Social:{social}");

                    // Total Secured Marks — Whole Number field
                    int totalSecured = telugu + hindi + english + maths + science + social;
                    target["cr675_totalsecuredmarks"] = totalSecured;
                    tracingService.Trace($"Total Secured Marks: {totalSecured}");

                    // Total Marks — Whole Number field
                    int totalMarks = GetMarkValue(target, "cr675_totalmarks");

                    // Percentage — Decimal field: shaik_percentage
                    decimal percentage = 0;
                    if (totalMarks > 0)
                    {
                        percentage = Math.Round(
                            ((decimal)totalSecured / (decimal)totalMarks) * 100, 2);
                        target["shaik_percentage"] = percentage;

                        tracingService.Trace(
                            $"Total: {totalMarks}, Percentage: {percentage}%");
                    }

                    // =============================================
                    // REQUIREMENT 3: Pass/Fail based on each
                    // subject marks >= 35
                    // If ANY subject < 35 → FAIL
                    // ALL subjects >= 35  → PASS
                    // =============================================

                    // Check each subject individually
                    bool teluguPass = telugu >= 35;
                    bool hindiPass = hindi >= 35;
                    bool englishPass = english >= 35;
                    bool mathsPass = maths >= 35;
                    bool sciencePass = science >= 35;
                    bool socialPass = social >= 35;

                    // Student passes only if ALL subjects >= 35
                    bool isAllPassed = teluguPass && hindiPass && englishPass &&
                                       mathsPass && sciencePass && socialPass;

                    if (isAllPassed)
                    {
                        target["shaik_result"] = "Pass";
                        tracingService.Trace("Result: Pass — All subjects >= 35");
                    }
                    else
                    {
                        target["shaik_result"] = "Fail";

                        // Log which subjects failed
                        if (!teluguPass) tracingService.Trace($"Telugu Failed: {telugu}");
                        if (!hindiPass) tracingService.Trace($"Hindi Failed: {hindi}");
                        if (!englishPass) tracingService.Trace($"English Failed: {english}");
                        if (!mathsPass) tracingService.Trace($"Maths Failed: {maths}");
                        if (!sciencePass) tracingService.Trace($"Science Failed: {science}");
                        if (!socialPass) tracingService.Trace($"Social Failed: {social}");

                        tracingService.Trace("Result: Fail — One or more subjects < 35");
                    }
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"Error in StudentAutoFieldsPlugin: {ex.Message}");
                    throw new InvalidPluginExecutionException(
                        $"StudentPlugin Error: {ex.Message}", ex);
                }
            }
        }

        private string GenerateHallTicketNo(IOrganizationService service,
                                             ITracingService tracingService)
        {
            int year = DateTime.Now.Year;
            string prefix = $"SSC{year}-";

            Random rnd = new Random();
            string hallTicket = "";
            bool isUnique = false;
            int attempts = 0;

            while (!isUnique && attempts < 100)
            {
                int number = rnd.Next(1000, 9999);
                hallTicket = $"{prefix}{number}";

                QueryExpression query = new QueryExpression("cr675_studentform");
                query.Criteria.AddCondition("shaik_hallticetno",
                    ConditionOperator.Equal, hallTicket);
                query.ColumnSet = new ColumnSet("shaik_hallticetno");

                EntityCollection results = service.RetrieveMultiple(query);

                if (results.Entities.Count == 0)
                    isUnique = true;

                attempts++;
            }

            return hallTicket;
        }

        // Returns int — for Whole Number fields
        private int GetMarkValue(Entity entity, string fieldName)
        {
            if (entity.Contains(fieldName) && entity[fieldName] != null)
            {
                return Convert.ToInt32(entity[fieldName]);
            }
            return 0;
        }
    }
}