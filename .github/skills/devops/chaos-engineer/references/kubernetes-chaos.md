# Kubernetes Chaos Reference

> **Load when:** Designing pod, node, or Litmus-based chaos experiments in Kubernetes.

## Pod-Level Chaos

### Pod Kill Experiment

Test that your service recovers gracefully when pods are terminated.

```bash
# Manual pod kill
kubectl delete pod escrow-api-7b9f8c6d4-abc12 -n escrow --grace-period=0

# Random pod kill using kubectl
kubectl get pods -n escrow -l app=escrow-api -o name | shuf -n 1 | xargs kubectl delete -n escrow

# Kill with grace period (simulates normal shutdown)
kubectl delete pod <name> -n escrow --grace-period=30
```

### Pod Resource Stress

```yaml
# stress-test-pod.yaml — Consume resources in a target namespace
apiVersion: v1
kind: Pod
metadata:
  name: stress-test
  namespace: escrow
spec:
  containers:
    - name: stress
      image: progrium/stress
      command: ["stress"]
      args: ["--cpu", "2", "--vm", "1", "--vm-bytes", "512M", "--timeout", "60s"]
      resources:
        requests:
          cpu: "2"
          memory: "512Mi"
        limits:
          cpu: "2"
          memory: "512Mi"
```

## Litmus Chaos Experiments

### Install Litmus

```bash
# Install LitmusChaos operator
kubectl apply -f https://litmuschaos.github.io/litmus/litmus-operator-v3.0.0.yaml

# Verify installation
kubectl get pods -n litmus
```

### Pod Delete Experiment

```yaml
# pod-delete-experiment.yaml
apiVersion: litmuschaos.io/v1alpha1
kind: ChaosEngine
metadata:
  name: escrow-pod-delete
  namespace: escrow
spec:
  appinfo:
    appns: escrow
    applabel: "app=escrow-api"
    appkind: deployment
  engineState: active
  chaosServiceAccount: litmus-admin
  experiments:
    - name: pod-delete
      spec:
        components:
          env:
            - name: TOTAL_CHAOS_DURATION
              value: "30"
            - name: CHAOS_INTERVAL
              value: "10"
            - name: FORCE
              value: "false"
            - name: PODS_AFFECTED_PERC
              value: "50"
        probe:
          - name: escrow-api-healthcheck
            type: httpProbe
            httpProbe/inputs:
              url: "http://escrow-api.escrow.svc:8080/health"
              method:
                get:
                  criteria: ==
                  responseCode: "200"
            mode: Continuous
            runProperties:
              probeTimeout: 5
              interval: 5
              retry: 3
```

### Network Chaos Experiment

```yaml
# network-loss-experiment.yaml
apiVersion: litmuschaos.io/v1alpha1
kind: ChaosEngine
metadata:
  name: escrow-network-loss
  namespace: escrow
spec:
  appinfo:
    appns: escrow
    applabel: "app=escrow-api"
    appkind: deployment
  engineState: active
  chaosServiceAccount: litmus-admin
  experiments:
    - name: pod-network-loss
      spec:
        components:
          env:
            - name: TOTAL_CHAOS_DURATION
              value: "60"
            - name: NETWORK_INTERFACE
              value: "eth0"
            - name: NETWORK_PACKET_LOSS_PERCENTAGE
              value: "50"
            - name: DESTINATION_IPS
              value: "10.0.0.100"  # PostgreSQL service IP
            - name: DESTINATION_HOSTS
              value: "postgres.escrow.svc.cluster.local"
```

### Container Kill Experiment

```yaml
# container-kill-experiment.yaml
apiVersion: litmuschaos.io/v1alpha1
kind: ChaosEngine
metadata:
  name: escrow-container-kill
  namespace: escrow
spec:
  appinfo:
    appns: escrow
    applabel: "app=escrow-api"
    appkind: deployment
  engineState: active
  chaosServiceAccount: litmus-admin
  experiments:
    - name: container-kill
      spec:
        components:
          env:
            - name: TARGET_CONTAINER
              value: "escrow-api"
            - name: TOTAL_CHAOS_DURATION
              value: "30"
            - name: CHAOS_INTERVAL
              value: "10"
            - name: SIGNAL
              value: "SIGKILL"
```

## Node-Level Chaos

### Node Drain (Simulates Node Failure)

```bash
# Cordon node (prevent new pods from scheduling)
kubectl cordon worker-node-3

# Drain node (evict all pods gracefully)
kubectl drain worker-node-3 --ignore-daemonsets --delete-emptydir-data --grace-period=30

# Verify pods rescheduled to other nodes
kubectl get pods -n escrow -o wide

# Uncordon when done
kubectl uncordon worker-node-3
```

### Node Resource Exhaustion

```yaml
# Litmus node-memory-hog experiment
apiVersion: litmuschaos.io/v1alpha1
kind: ChaosEngine
metadata:
  name: escrow-node-memory-hog
  namespace: escrow
spec:
  engineState: active
  chaosServiceAccount: litmus-admin
  experiments:
    - name: node-memory-hog
      spec:
        components:
          env:
            - name: TOTAL_CHAOS_DURATION
              value: "60"
            - name: MEMORY_PERCENTAGE
              value: "80"
            - name: TARGET_NODES
              value: "worker-node-2"
```

## Kubernetes Health Check Validation

Ensure your health checks are properly configured before running chaos:

```yaml
# deployment.yaml — Proper health check configuration
apiVersion: apps/v1
kind: Deployment
metadata:
  name: escrow-api
  namespace: escrow
spec:
  replicas: 3
  template:
    spec:
      containers:
        - name: escrow-api
          image: nextruzt/escrow-api:latest
          ports:
            - containerPort: 8080
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 5
            failureThreshold: 3
          startupProbe:
            httpGet:
              path: /health/startup
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 5
            failureThreshold: 30
```

### ASP.NET Core Health Checks for Kubernetes

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = _ => true
});
```

## Cleanup After Experiments

```bash
# Delete all Litmus chaos engines in namespace
kubectl delete chaosengine --all -n escrow

# Verify no lingering chaos pods
kubectl get pods -n escrow | grep -i chaos

# Check application pods are healthy
kubectl get pods -n escrow -l app=escrow-api

# Verify service endpoints
kubectl get endpoints escrow-api -n escrow
```
