# RabbitMQ AKS Production Strategy

This project keeps RabbitMQ for the AKS backend deployment. The target approach is RabbitMQ running inside AKS, installed and operated with a production-oriented distribution rather than a hand-written single-container manifest.

## Decision

Use one of these managed-in-cluster approaches in a later PR:

- RabbitMQ Cluster Operator; or
- Bitnami RabbitMQ Helm chart.

Do not vendor a Helm chart into this repository. Do not model production RabbitMQ as a simple Docker Compose-style `rabbitmq:3-management` container.

## Why Not A Simple Container

The local Docker Compose RabbitMQ container is useful for development, but it does not cover production needs:

- durable storage lifecycle;
- stable identity and clustering;
- credential and secret rotation;
- readiness/liveness behavior;
- upgrade and rollback mechanics;
- resource requests and limits;
- network isolation;
- backup and restore process;
- operational monitoring and alerts.

## Application Expectations

InventoryService and ShipmentService read RabbitMQ settings from configuration:

- `RabbitMq__HostName`
- `RabbitMq__Port`
- `RabbitMq__UserName`
- `RabbitMq__Password`
- `RabbitMq__ExchangeName`

The shared RabbitMQ infrastructure validates required settings at startup, declares durable exchanges/queues, configures dead-letter queues, uses manual acknowledgements, and exposes RabbitMQ readiness through service health checks.

The AKS skeleton keeps only RabbitMQ configuration references. It does not deploy RabbitMQ.

## AKS Requirements

The future RabbitMQ deployment must provide:

- durable PersistentVolumeClaims for broker data;
- a non-guest username and password;
- credentials supplied from AKS secrets, Key Vault, or a secrets operator, not source control;
- internal Kubernetes service access only;
- no public management UI exposure;
- readiness and liveness probes;
- resource requests and limits;
- upgrade and rollback plan;
- backup and restore runbook;
- monitoring and alerts for queue depth, consumer availability, connection failures, and disk pressure;
- TLS plan for broker connections if required by the production security model;
- NetworkPolicy or equivalent network controls when cluster networking policy is enabled.

## Current Local Development

Local Docker Compose remains unchanged:

- RabbitMQ uses `rabbitmq:3-management`;
- local app containers use service DNS name `rabbitmq`;
- local management UI is exposed on `localhost:15672`;
- local default credentials are development-only.

Do not use the local Compose credentials or management exposure pattern for production.

## Follow-Up PR

The RabbitMQ deployment PR should choose Cluster Operator or Bitnami Helm, then add the smallest deployment assets needed to wire:

- internal RabbitMQ service DNS;
- durable storage class/PVC settings;
- secrets or Key Vault integration;
- resource requests and limits;
- probe settings;
- management UI access policy;
- monitoring hooks;
- backup/restore notes.
