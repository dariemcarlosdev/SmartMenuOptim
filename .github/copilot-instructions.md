# Repository Instructions

## Code Review Guidelines
- **Security**: Flag hardcoded secrets, API keys, and insecure input handling.
- **Style**: Enforce [Conventional Commits](https://www.conventionalcommits.org) and ensure all functions have JSDoc/Type hints.
- **Logic**: Identify missing edge cases, redundant loops, and complex conditional checks.
- **Testing**: Require unit tests for new logic; suggest specific test cases if missing.

## Code Review Instructions
When performing a code review, follow these specific guidelines:

*   **Identify Issues:** Highlight potential issues inline as comments.
*   **Risk Assessment:** Include a risk assessment with one of these levels: Very Low, Low, Medium, High, or Very High, for any significant issues found.
*   **Provide Suggestions:** Offer concrete, actionable suggestions for improvement, not just critiques.
*   **Context:** Ensure suggestions are relevant to the existing codebase's patterns.
*   **Non-Blocking:** Remember that Copilot reviews are comments and do not block pull request merges.

# Code Review Rules
- Flag any missing unit tests for new logic.
- Check for hardcoded credentials or API keys.
- Ensure error handling follows the project's 'Result' pattern.

  ## Commit Message Instructions
When generating a commit message:

*   **Format:** Use the [Conventional Commits](https://www.conventionalcommits.org) specification (e.g., `feat:`, `fix:`, `docs:`, `style:`).
*   **Clarity:** The message should clearly describe what changed and why, in the imperative tense.
*   **Length:** The subject line should be under 50 characters, and the body (if needed) should wrap at 72 characters.

## Commit Message Formatting
When generating commit messages, strictly follow this format:
- **Format**: `<type>(<scope>): <description>`
- **Types**: feat, fix, docs, style, refactor, perf, test, chore, ci.
- **Rules**:
  - Use the imperative mood (e.g., "Add feature" not "Added feature").
  - Limit the subject line to 50 characters.
  - Include a blank line before the body if additional context is needed.
  - Use bullet points for multiple changes in the body.
  
