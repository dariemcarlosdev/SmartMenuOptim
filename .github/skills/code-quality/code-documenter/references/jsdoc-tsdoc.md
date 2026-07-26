# JSDoc / TSDoc Reference

Guide for generating JavaScript and TypeScript documentation comments.

## TSDoc Standard (TypeScript)

### Function Documentation
```typescript
/**
 * Calculates the escrow fee based on transaction amount and escrow type.
 *
 * @param amount - The transaction amount in the specified currency.
 * @param escrowType - The type of escrow determining the fee schedule.
 * @returns The calculated fee as a Money value object.
 * @throws {@link InvalidAmountError} When amount is negative or zero.
 *
 * @example
 * ```ts
 * const fee = calculateEscrowFee(Money.usd(1000), EscrowType.Standard);
 * // Returns Money.usd(25) — 2.5% standard rate
 * ```
 *
 * @remarks
 * Fee calculation follows a tiered structure:
 * - Standard: 2.5% of transaction amount
 * - Premium: 1.5% with minimum $10
 * - Enterprise: Custom rate from contract
 *
 * @see {@link FeeSchedule} for rate configuration
 */
export function calculateEscrowFee(
  amount: Money,
  escrowType: EscrowType
): Money {
```

### Interface Documentation
```typescript
/**
 * Contract for escrow repository operations.
 *
 * @remarks
 * All methods accept an optional `AbortSignal` for cancellation.
 * Implementations must ensure transactional consistency for write operations.
 *
 * @example
 * ```ts
 * const repo: IEscrowRepository = container.resolve('IEscrowRepository');
 * const escrow = await repo.findById(escrowId);
 * ```
 */
export interface IEscrowRepository {
  /**
   * Retrieves an escrow by its unique identifier.
   *
   * @param id - The escrow's unique identifier.
   * @param signal - Optional abort signal for cancellation.
   * @returns The escrow if found, or `null` if no match exists.
   */
  findById(id: EscrowId, signal?: AbortSignal): Promise<Escrow | null>;

  /**
   * Persists a new escrow transaction.
   *
   * @param escrow - The escrow entity to create.
   * @returns The created escrow with server-assigned fields populated.
   * @throws {@link DuplicateError} When an escrow with the same ID exists.
   */
  create(escrow: Escrow): Promise<Escrow>;
}
```

### Class Documentation
```typescript
/**
 * Manages escrow lifecycle state transitions with validation.
 *
 * @remarks
 * This class implements the State pattern for escrow lifecycle management.
 * Invalid transitions throw {@link InvalidStateTransitionError}.
 *
 * @example
 * ```ts
 * const manager = new EscrowStateManager(escrow);
 * await manager.transition(EscrowAction.Fund, { amount: Money.usd(500) });
 * ```
 */
export class EscrowStateManager {
```

### Type Alias and Enum
```typescript
/**
 * Unique identifier for an escrow transaction.
 * Format: `ESC-{UUID}` (e.g., `ESC-550e8400-e29b-41d4-a716-446655440000`).
 */
export type EscrowId = Brand<string, 'EscrowId'>;

/**
 * Lifecycle states of an escrow transaction.
 */
export enum EscrowStatus {
  /** Escrow created but not yet funded. */
  Draft = 'DRAFT',
  /** Buyer has deposited funds. */
  Funded = 'FUNDED',
  /** Funds released to seller. */
  Released = 'RELEASED',
  /** Under dispute; funds held. */
  Disputed = 'DISPUTED',
}
```

## JSDoc (JavaScript)

### Function with Type Annotations
```javascript
/**
 * Validates an escrow creation request against business rules.
 *
 * @param {Object} request - The creation request.
 * @param {string} request.buyerId - UUID of the buyer.
 * @param {string} request.sellerId - UUID of the seller.
 * @param {number} request.amount - Transaction amount (positive).
 * @param {string} request.currency - ISO 4217 currency code.
 * @returns {{ valid: boolean, errors: string[] }} Validation result.
 *
 * @example
 * const result = validateEscrowRequest({
 *   buyerId: '123', sellerId: '456', amount: 500, currency: 'USD'
 * });
 * if (!result.valid) console.error(result.errors);
 */
function validateEscrowRequest(request) {
```

## Key TSDoc Tags Reference

| Tag | Usage |
|-----|-------|
| `@param name - desc` | Document a parameter |
| `@returns desc` | Document return value |
| `@throws {@link ErrorType}` | Document thrown error |
| `@example` | Code example (fenced code block) |
| `@remarks` | Additional details beyond summary |
| `@see {@link Type}` | Cross-reference to related type |
| `@deprecated desc` | Mark as deprecated with migration path |
| `@alpha` / `@beta` | API stability markers |
| `@internal` | Not part of public API |
| `@readonly` | Property is read-only |
| `@defaultValue val` | Default value for optional parameter |

## Common Mistakes

| ❌ Mistake | ✅ Correct |
|-----------|----------|
| `@param {string} name The name` | `@param name - The name` (TSDoc style) |
| Missing `@throws` for error cases | Always document thrown errors |
| No `@example` for complex APIs | Add runnable example code |
| Documenting obvious getters | Skip trivial self-explanatory members |
| `@returns {void}` | Omit `@returns` for void functions |
