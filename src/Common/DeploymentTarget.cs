namespace Cmf.CustomerPortal.Sdk.Common
{
    public enum DeploymentTarget
    {
        dockerswarm,
        portainer,
        KubernetesOnPremisesTarget,
        KubernetesRemoteTarget,
        OpenShiftOnPremisesTarget,
        OpenShiftRemoteTarget,
        AzureKubernetesServiceTarget
    }
}
