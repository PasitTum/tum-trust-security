# 🟢 Setup Guide: Using Subagents with ChatGPT

This guide explains how to use the TUM Global Subagents with **ChatGPT Plus / Team / Enterprise (Custom GPTs)**, and the **OpenAI API / Assistants API**.

---

## 1. Creating Custom GPTs (Web UI)

You can create dedicated GPTs for key pillars or agents (e.g. `TUM DX Lead`, `TUM Tech Lead`, `TUM Tri-Temporal Research`, `TUM Red Team`):

1. Go to **Explore GPTs** -> **Create**.
2. Go to the **Configure** tab:
   - **Name:** e.g., `TUM DX Lead & SDK Usability`
   - **Description:** Frictionless SDK integration architect for global identity verification.
   - **Instructions:** Open the corresponding markdown file (e.g., `.subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md`), copy the content from `## 📋 System Instructions (Copy-Paste Ready)`, and paste it here.
   - **Conversation Starters:**
     * `How do we reduce Time-to-First-Verification under 5 minutes?`
     * `Audit our ThaID C# sample code for developer clarity.`
     * `Design user-friendly error codes for network timeouts.`
   - **Knowledge:** Upload `README.md`, `ARCHITECTURE.md`, and SDK interface specifications.
   - **Capabilities:** Enable *Code Interpreter* and *Web Browsing* as needed.

---

## 2. Using Single-Prompt Custom Instructions (All-In-One ChatGPT)

If you prefer using regular ChatGPT without creating multiple GPTs:

1. Open **Settings** -> **Custom Instructions**.
2. In **How would you like ChatGPT to respond?**, paste:

```markdown
You are part of the TUM Global Identity & AI Kiosk Ecosystem engineering team.
You support 33 specialized roles across 8 pillars (Executive, Tri-Temporal Research, Product, DX/Architecture, Polyglot Engineering, Hardware/QA, AI Lifecycle, Security/Legal).

When prompted with a role tag (e.g. [Role: DX Lead], [Role: CTO], [Role: Legacy Researcher], [Role: Red Team]):
1. Adopt that specific role's mindset, domain expertise, and rigorous standards.
2. Adhere to our core mission: Zero-Friction global developer onboarding, high kiosk reliability, and rock-solid legal/cryptographic compliance.
3. If no role is specified, respond as the Cross-Platform Software Architect.
```

3. In Chat, trigger any agent instantly:
   - `[Role: Legacy Researcher] What lessons can we learn from 2000s Smart Card readers with USB sleep states?`
   - `[Role: Applied AI Engineer] How do we compress a face recognition ONNX model under 100MB for kiosk memory?`

---

## 3. Using with OpenAI Assistants API (Python Example)

```python
from openai import OpenAI
import pathlib

client = OpenAI()

# Load agent system instruction
prompt_path = pathlib.Path(".subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md")
system_prompt = prompt_path.read_text(encoding="utf-8")

assistant = client.beta.assistants.create(
    name="TUM DX Lead",
    instructions=system_prompt,
    model="gpt-4o",
    tools=[{"type": "code_interpreter"}]
)

print(f"Assistant created: {assistant.id}")
```
