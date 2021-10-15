using System;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class NewEnvironmentUtilities : INewEnvironmentUtilities
    {
        public string GetDeploymentTargetValue(string abbreviatedDeploymentTarget)
        {
            switch (abbreviatedDeploymentTarget)
            {
                case "portainer":
                    return "PortainerV2Target";
                case "dockerswarm":
                    return "DockerSwarmOnPremisesTarget";
                default:
                    throw new Exception($"Target parameter '{abbreviatedDeploymentTarget}' not recognized");
            }
        }
    }
}
