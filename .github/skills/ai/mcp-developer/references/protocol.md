# MCP Protocol Reference

> **Load when:** Understanding JSON-RPC 2.0 message types, MCP lifecycle, or protocol details.

## Protocol Overview

The Model Context Protocol (MCP) uses JSON-RPC 2.0 as its wire format. Communication follows a client-server model where AI assistants (clients) connect to capability providers (servers).

```
┌──────────────┐                    ┌──────────────┐
│  AI Client   │  ← JSON-RPC 2.0 → │  MCP Server  │
│ (Claude, etc)│                    │ (Your Code)  │
└──────────────┘                    └──────────────┘
```

## Message Types

### Request (Client → Server or Server → Client)

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
        "name": "create-escrow",
        "arguments": {
            "buyerId": "USR-001",
            "sellerId": "USR-002",
            "amountCents": 500000,
            "currency": "USD"
        }
    }
}
```

### Response (Success)

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "content": [
            {
                "type": "text",
                "text": "Escrow ESC-12345 created: $5,000.00 USD from USR-001 to USR-002"
            }
        ]
    }
}
```

### Response (Error)

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "error": {
        "code": -32602,
        "message": "Invalid params: buyerId is required",
        "data": {
            "field": "buyerId",
            "constraint": "required"
        }
    }
}
```

### Notification (No Response Expected)

```json
{
    "jsonrpc": "2.0",
    "method": "notifications/progress",
    "params": {
        "progressToken": "op-123",
        "progress": 50,
        "total": 100
    }
}
```

## Standard JSON-RPC Error Codes

| Code | Name | Description |
|---|---|---|
| -32700 | Parse error | Invalid JSON |
| -32600 | Invalid request | Missing required fields |
| -32601 | Method not found | Unknown method name |
| -32602 | Invalid params | Parameter validation failed |
| -32603 | Internal error | Server-side error |

## MCP Lifecycle

### 1. Initialize

Client sends capabilities and receives server capabilities:

```json
// Client → Server
{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
        "protocolVersion": "2024-11-05",
        "capabilities": {
            "roots": { "listChanged": true }
        },
        "clientInfo": {
            "name": "copilot-cli",
            "version": "1.0.0"
        }
    }
}

// Server → Client
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "protocolVersion": "2024-11-05",
        "capabilities": {
            "tools": {},
            "resources": { "subscribe": true },
            "prompts": {}
        },
        "serverInfo": {
            "name": "escrow-mcp-server",
            "version": "1.0.0"
        }
    }
}
```

### 2. Initialized Notification

```json
// Client → Server (notification — no id)
{
    "jsonrpc": "2.0",
    "method": "notifications/initialized"
}
```

### 3. Capability Discovery

```json
// List available tools
{ "jsonrpc": "2.0", "id": 2, "method": "tools/list" }

// List available resources
{ "jsonrpc": "2.0", "id": 3, "method": "resources/list" }

// List available prompts
{ "jsonrpc": "2.0", "id": 4, "method": "prompts/list" }
```

### 4. Tool Invocation

```json
// Client calls a tool
{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
        "name": "get-escrow-status",
        "arguments": { "escrowId": "ESC-12345" }
    }
}
```

### 5. Resource Access

```json
// Client reads a resource
{
    "jsonrpc": "2.0",
    "id": 6,
    "method": "resources/read",
    "params": {
        "uri": "escrow://ESC-12345"
    }
}
```

## Content Types

MCP supports multiple content types in tool responses:

```json
// Text content
{ "type": "text", "text": "Escrow created successfully" }

// Image content
{ "type": "image", "data": "base64...", "mimeType": "image/png" }

// Resource reference
{ "type": "resource", "resource": { "uri": "escrow://ESC-12345", "mimeType": "application/json", "text": "{...}" } }
```

## Transport Protocols

### stdio (Standard I/O)

Default for CLI tools. Messages are sent as newline-delimited JSON over stdin/stdout.

```
Client stdin  → Server stdout
Server stdin  ← Client stdout
Server stderr → Logging (not protocol)
```

### HTTP + Server-Sent Events (SSE)

For web-based servers. Client sends POST requests, server streams events via SSE.

```
Client → POST /message  (JSON-RPC request)
Server → SSE /events    (JSON-RPC responses + notifications)
```

### Streamable HTTP

Newer transport for full-duplex communication over HTTP:

```
Client → POST /mcp  (JSON-RPC request in body)
Server → 200 OK     (JSON-RPC response in body)
         OR
Server → 200 OK     (SSE stream for multiple responses)
```

## Protocol Version Negotiation

```
Client: "I support protocol version 2024-11-05"
Server: "I also support 2024-11-05, let's use that"
         OR
Server: "I don't support that version" → Error
```

The client and server must agree on a protocol version during initialization. If they don't share a compatible version, the connection fails.
