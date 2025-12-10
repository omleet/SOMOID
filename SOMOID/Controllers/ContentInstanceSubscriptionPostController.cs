using SOMOID.Helpers;
using SOMOID.Models;
using SOMOID.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;


namespace SOMOID.Controllers
{
    public class ContentInstanceSubscriptionPostController : ApiController
    {
        private readonly SQLHelper sqlHelper = new SQLHelper();
        public OperationResult CreateContentInstance(string appName, string containerName, ContentInstance ci)
        {
            ci.ApplicationResourceName = appName;
            ci.ContainerResourceName = containerName;
            ci.CreationDatetime = DateTime.UtcNow;

            if (!sqlHelper.ContentInstanceParentExists(appName, containerName))
                return new OperationResult { Success = false, Error = "Parent container not found" };

            if (sqlHelper.ContentInstanceExistsInContainer(appName, containerName, ci.ResourceName))
                return new OperationResult { Success = false, Error = "Content instance already exists" };

            bool created = sqlHelper.InsertContentInstance(ci);
            if (!created) return new OperationResult { Success = false, Error = "Failed to insert content instance" };

            return new OperationResult { Success = true };
        }

        public OperationResult CreateSubscription(string appName, string containerName, Subscription sub)
        {
            var validator = new SubscriptionValidator();
            var errors = validator.Validate(sub);

            if (errors.Any())
                return new OperationResult { Success = false, Error = "Validation failed", ValidationErrors = errors };

            sub.ApplicationResourceName = appName;
            sub.ContainerResourceName = containerName;
            sub.CreationDatetime = DateTime.UtcNow;

            int containerCount = sqlHelper.CheckIfSubscriptionParentExists(appName, containerName);
            if (containerCount == 0)
                return new OperationResult { Success = false, Error = "Parent container not found" };

            int subCount = sqlHelper.CheckIfSubscriptionAlreadyExists(appName, containerName, sub.ResourceName);
            if (subCount > 0)
                return new OperationResult { Success = false, Error = "Subscription already exists" };

            int rowsAffected = sqlHelper.InsertNewSubscription(
                sub.ResourceName,
                sub.CreationDatetime,
                containerName,
                sub.ApplicationResourceName,
                sub.Evt,
                sub.Endpoint
            );

            if (rowsAffected == 0)
                return new OperationResult { Success = false, Error = "Failed to insert subscription" };

            return new OperationResult { Success = true };
        }
    }

}
