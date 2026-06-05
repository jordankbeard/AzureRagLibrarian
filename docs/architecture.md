# Architecture

Azure RAG Librarian is split into small services so the cloud setup and the console workflow can be understood independently.

```mermaid
sequenceDiagram
    participant User
    participant CLI as Console app
    participant Config as RagOptions
    participant Index as DocumentIndexService
    participant Agent as AgentProvisioningService
    participant Chat as RagChatSession
    participant Azure as Azure AI Foundry

    User->>CLI: Start app
    CLI->>Config: Load and validate settings
    CLI->>Azure: Create AIProjectClient
    CLI->>Index: Ensure document and vector store
    Index->>Azure: Reuse or upload file
    Index->>Azure: Reuse or create vector store
    CLI->>Agent: Ensure project agent
    Agent->>Azure: Reuse or register agent version
    User->>Chat: Ask questions
    Chat->>Azure: Create responses with prior response id
    Azure-->>User: Document-grounded answer
```

## Main components

- `Program.cs` coordinates startup, validation, indexing, agent provisioning, and chat.
- `RagOptions` loads required and optional settings from JSON, user secrets, and environment variables.
- `DocumentIndexService` owns Foundry file upload and vector store creation/reuse.
- `AgentProvisioningService` owns project agent lookup and registration.
- `RagChatSession` owns the interactive conversation loop.

Unit tests cover the local behavior that should not require an Azure subscription.
