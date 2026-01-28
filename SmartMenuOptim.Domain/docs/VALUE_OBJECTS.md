# Value Objects - Domain-Driven Design

## Table of Contents
- [What are Value Objects?](#what-are-value-objects)
- [Characteristics of Value Objects](#characteristics-of-value-objects)
- [DDD Best Practices](#ddd-best-practices)
- [Value Objects vs Entities](#value-objects-vs-entities)
- [Implementation in C# 12 / .NET 8](#implementation-in-c-12--net-8)
- [Value Objects in This Project](#value-objects-in-this-project)
- [Usage in Domain Entities](#usage-in-domain-entities)
- [Benefits](#benefits)
- [Common Patterns](#common-patterns)
- [Testing Value Objects](#testing-value-objects)

---

## What are Value Objects?

**Value Objects** are a fundamental building block in Domain-Driven Design (DDD). They represent concepts in your domain that are defined by their attributes rather than a unique identity. Unlike entities, two value objects with the same values are considered equal and interchangeable.

### Key Concept
> "An object that represents a descriptive aspect of the domain with no conceptual identity is called a VALUE OBJECT."
> — Eric Evans, Domain-Driven Design

---

## Characteristics of Value Objects

### 1. **Immutability**
Once created, a value object cannot be modified. Any change requires creating a new instance.

```csharp
// ✅ CORRECT: Immutable value object
var originalEmail = new Email("user@example.com");
var newEmail = new Email("newuser@example.com"); // Create new instance

// ❌ WRONG: Mutable value object
originalEmail.Value = "changed@example.com"; // Not possible - no setter
```

### 2. **Value Equality**
Two value objects are equal if all their properties have the same values.

```csharp
var money1 = new Money(10.50m, "USD");
var money2 = new Money(10.50m, "USD");
var money3 = new Money(10.50m, "EUR");

Assert.True(money1 == money2);  // ✅ Equal - same amount and currency
Assert.False(money1 == money3); // ❌ Not equal - different currency
```

### 3. **Self-Validation**
Value objects validate themselves upon creation, ensuring they're always in a valid state.

```csharp
// ✅ Valid email
var validEmail = new Email("user@example.com");

// ❌ Throws ArgumentException - invalid format
var invalidEmail = new Email("not-an-email"); 
```

### 4. **No Identity**
Value objects don't have a unique identifier. They're identified by their values.

```csharp
// Both addresses are the same if all properties match
var address1 = new Address("123 Main St", "Springfield", "IL", "62701", "US");
var address2 = new Address("123 Main St", "Springfield", "IL", "62701", "US");

Assert.True(address1 == address2); // No need for ID comparison
```

### 5. **Conceptual Whole**
A value object represents a complete concept, not just a primitive wrapper.

```csharp
// ❌ Primitive obsession
string email = "user@example.com";
string phone = "123-456-7890";
decimal amount = 10.50m;
string currency = "USD";

// ✅ Rich domain model with value objects
Email email = new Email("user@example.com");
PhoneNumber phone = new PhoneNumber("123-456-7890");
Money price = new Money(10.50m, "USD");
```

---

## DDD Best Practices

### 1. **Encapsulate Domain Logic**
Value objects should contain business rules and validations related to their concept.

```csharp
public sealed record Percentage
{
    public decimal Value { get; }
    
    public Percentage(decimal value, bool isWholeNumber = false)
    {
        // Encapsulated validation
        if (isWholeNumber)
        {
            if (value < 0 || value > 100)
                throw new ArgumentException("Percentage must be between 0 and 100.");
            Value = value / 100;
        }
        else
        {
            if (value < 0 || value > 1)
                throw new ArgumentException("Percentage must be between 0.0 and 1.0.");
            Value = value;
        }
    }
    
    // Domain operations
    public decimal ApplyDiscount(decimal amount) => amount * (1 - Value);
    public decimal ApplyMarkup(decimal amount) => amount * (1 + Value);
}
```

### 2. **Make Implicit Concepts Explicit**
Replace primitive types with meaningful value objects that express domain concepts.

```csharp
// ❌ Before: Primitive obsession
public class MenuItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public decimal DiscountRate { get; set; }
}

// ✅ After: Explicit domain concepts
public class MenuItem
{
    public string Name { get; set; }
    public Money Price { get; set; }
    public Percentage Discount { get; set; }
}
```

### 3. **Maintain Invariants**
Ensure value objects are always in a valid state through constructor validation.

```csharp
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        // Invariants enforced
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.");
        
        if (!EmailRegex.IsMatch(value.Trim()))
            throw new ArgumentException($"'{value}' is not a valid email.");
        
        Value = value.Trim();
    }
}
```

### 4. **Provide Domain Operations**
Include methods that perform domain-specific operations.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    // Domain operations
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public Money ApplyDiscount(Percentage discount) =>
        new Money(discount.ApplyDiscount(Amount), Currency);

    public bool IsPositive => Amount > 0;
    public bool IsZero => Amount == 0;
}
```

---

## Value Objects vs Entities

| Aspect | Value Object | Entity |
|--------|-------------|--------|
| **Identity** | No unique identifier | Has unique identifier (ID) |
| **Equality** | Based on all property values | Based on ID only |
| **Mutability** | Immutable | Mutable |
| **Lifespan** | Can be created/discarded freely | Has a lifecycle |
| **Example** | Email, Money, Address | Customer, Restaurant, Order |

```csharp
// Entity - has identity and lifecycle
public class Restaurant : EntityBase
{
    public int Id { get; set; } // Identity
    public string Name { get; set; }
    public Address Location { get; set; } // Value Object
    public PhoneNumber Phone { get; set; } // Value Object
    
    // Entities are mutable
    public void UpdateLocation(Address newAddress)
    {
        Location = newAddress; // Replace entire value object
    }
}

// Value Object - no identity, immutable
public sealed record Address
{
    public string Street { get; }
    public string City { get; }
    // ... more properties
    
    // Immutable - create new instance for changes
    public Address WithStreet(string newStreet) =>
        new Address(newStreet, City, State, PostalCode, CountryCode, Street2);
}
```

---

## Implementation in C# 12 / .NET 8

### Using `record` Types (Recommended)

C# 9+ `record` types are perfect for value objects as they provide:
- Value-based equality by default
- Immutability by default
- Concise syntax
- Built-in `with` expressions for non-destructive mutation

```csharp
public sealed record Email
{
    public string Value { get; }
    public string NormalizedValue { get; }

    public Email(string value)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.");
        
        Value = value.Trim();
        NormalizedValue = value.ToLowerInvariant();
    }

    // Implicit conversion for convenience
    public static implicit operator string(Email email) => email.Value;
    
    public override string ToString() => Value;
}
```

### Alternative: Class with Value Semantics

For complex value objects that need mutable builder patterns or extensive logic:

```csharp
public sealed class Address : IEquatable<Address>
{
    public string Street { get; }
    public string City { get; }
    // ... properties

    public Address(/* parameters */)
    {
        // Initialize
    }

    // Implement value equality
    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Street == other.Street &&
               City == other.City &&
               State == other.State &&
               PostalCode == other.PostalCode &&
               CountryCode == other.CountryCode;
    }

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode() =>
        HashCode.Combine(Street, City, State, PostalCode, CountryCode);
}
```

---

## Value Objects in This Project

This project includes the following value objects:

### 1. **Email**
```csharp
var userEmail = new Email("customer@restaurant.com");
var normalizedEmail = userEmail.NormalizedValue; // For lookups
```

**Usage:**
- User email addresses
- Contact information
- Email verification

### 2. **PhoneNumber**
```csharp
var phone = new PhoneNumber("+1-555-123-4567");
var international = phone.ToInternationalFormat(); // +15551234567
```

**Usage:**
- Restaurant contact numbers
- Customer phone numbers
- Delivery contact information

### 3. **Money**
```csharp
var menuItemPrice = new Money(12.99m, "USD");
var taxAmount = new Money(1.04m, "USD");
var total = menuItemPrice + taxAmount; // Money(14.03, "USD")

var discountedPrice = menuItemPrice * 0.9m; // 10% off
```

**Usage:**
- Menu item prices
- Order totals
- Discounts and taxes
- Revenue calculations

### 4. **Percentage**
```csharp
var taxRate = Percentage.FromWholeNumber(8.5m); // 8.5%
var discount = Percentage.FromDecimal(0.15m);    // 15%

var taxAmount = taxRate.Of(100m);                // 8.50
var salePrice = discount.ApplyDiscount(100m);    // 85.00
```

**Usage:**
- Tax rates
- Discount percentages
- Service charges
- Tip percentages
- Commission rates

### 5. **Address**
```csharp
var restaurantAddress = new Address(
    street: "123 Main Street",
    city: "Springfield",
    state: "IL",
    postalCode: "62701",
    countryCode: "US",
    street2: "Suite 100"
);

var formatted = restaurantAddress.ToFormattedString();
var oneLine = restaurantAddress.ToSingleLine();
```

**Usage:**
- Restaurant locations
- Delivery addresses
- Billing addresses
- Corporate addresses

---

## Usage in Domain Entities

### Example 1: Restaurant Entity

```csharp
public class Restaurant : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Value Objects
    public Address Location { get; set; }
    public PhoneNumber ContactPhone { get; set; }
    public Email ContactEmail { get; set; }
    
    // Collections with Value Objects
    public List<BusinessHours> OperatingHours { get; set; }
    
    public void UpdateContactInfo(Email email, PhoneNumber phone)
    {
        ContactEmail = email;      // Replace value object
        ContactPhone = phone;      // Replace value object
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Example 2: MenuItem Entity

```csharp
public class MenuItem : EntityBase
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Value Objects for pricing
    public Money BasePrice { get; set; }
    public Percentage? Discount { get; set; }
    public Percentage TaxRate { get; set; }
    
    // Calculated property using value objects
    public Money FinalPrice
    {
        get
        {
            var price = BasePrice;
            
            if (Discount != null)
                price = new Money(Discount.ApplyDiscount(price.Amount), price.Currency);
            
            var tax = new Money(TaxRate.Of(price.Amount), price.Currency);
            return price + tax;
        }
    }
    
    public void ApplyDiscount(Percentage discount)
    {
        if (discount.Value > 0.5m) // Max 50% discount
            throw new InvalidOperationException("Discount cannot exceed 50%");
        
        Discount = discount;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Example 3: Customer Entity

```csharp
public class Customer : EntityBase
{
    public int Id { get; set; }
    public string FullName { get; set; }
    
    // Value Objects for contact
    public Email Email { get; set; }
    public PhoneNumber? Phone { get; set; }
    
    // Collection of Value Objects
    public List<Address> DeliveryAddresses { get; set; } = new();
    public Address? PreferredAddress { get; set; }
    
    public void AddDeliveryAddress(Address address)
    {
        if (!DeliveryAddresses.Contains(address)) // Value equality check
            DeliveryAddresses.Add(address);
    }
    
    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail; // Replace entire value object
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Example 4: Order Entity

```csharp
public class Order : EntityBase
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    
    // Value Objects
    public Address? DeliveryAddress { get; set; }
    public Money Subtotal { get; set; }
    public Money TaxAmount { get; set; }
    public Money DeliveryFee { get; set; }
    public Money TotalAmount { get; set; }
    public Percentage? DiscountApplied { get; set; }
    
    public void CalculateTotals(Percentage taxRate, Money deliveryFee)
    {
        // Use value object operations
        var subtotalWithDiscount = DiscountApplied != null
            ? new Money(DiscountApplied.ApplyDiscount(Subtotal.Amount), Subtotal.Currency)
            : Subtotal;
        
        TaxAmount = new Money(taxRate.Of(subtotalWithDiscount.Amount), Subtotal.Currency);
        DeliveryFee = deliveryFee;
        
        TotalAmount = subtotalWithDiscount + TaxAmount + DeliveryFee;
    }
}
```

### Example 5: StaffMember Entity

```csharp
public class StaffMember : EntityBase
{
    public int Id { get; set; }
    public string FullName { get; set; }
    
    // Value Objects
    public Email WorkEmail { get; set; }
    public PhoneNumber WorkPhone { get; set; }
    public Money HourlyRate { get; set; }
    public Percentage? CommissionRate { get; set; }
    
    public Money CalculateCommission(Money salesAmount)
    {
        if (CommissionRate == null)
            return Money.Zero(salesAmount.Currency);
        
        return new Money(
            CommissionRate.Of(salesAmount.Amount),
            salesAmount.Currency
        );
    }
}
```

---

## Benefits

### 1. **Type Safety**
```csharp
// ❌ Primitive obsession - easy to mix up
public void SetPrice(decimal amount, string currency) { }
SetPrice("USD", 10.50); // Compiles but wrong!

// ✅ Value object - compile-time safety
public void SetPrice(Money price) { }
SetPrice(new Money(10.50m, "USD")); // Type-safe
```

### 2. **Reduced Duplication**
Validation and business logic are centralized in the value object, not scattered across the codebase.

### 3. **Improved Readability**
```csharp
// ❌ Unclear primitive types
public bool ValidateDiscount(decimal discount)
{
    return discount >= 0 && discount <= 0.5m;
}

// ✅ Clear domain concept
public bool ValidateDiscount(Percentage discount)
{
    return discount.Value <= 0.5m; // Max 50%
}
```

### 4. **Domain Expressiveness**
The code reads like the business domain.

```csharp
var menuItem = new MenuItem
{
    Name = "Burger",
    BasePrice = new Money(8.99m, "USD"),
    Discount = Percentage.FromWholeNumber(10),
    TaxRate = Percentage.FromWholeNumber(8.5m)
};

var finalPrice = menuItem.FinalPrice; // Expressive and type-safe
```

### 5. **Testability**
Value objects are easy to test due to immutability and value equality.

---

## Common Patterns

### Pattern 1: Factory Methods
```csharp
public sealed record Money
{
    public static Money Zero(string currency) => new Money(0, currency);
    public static Money FromDollars(decimal dollars) => new Money(dollars, "USD");
}

var zeroDollars = Money.Zero("USD");
var tenDollars = Money.FromDollars(10);
```

### Pattern 2: Implicit Conversions
```csharp
public sealed record Email
{
    public string Value { get; }
    
    // Allow implicit conversion to string
    public static implicit operator string(Email email) => email.Value;
}

Email email = new Email("test@example.com");
string emailString = email; // Implicit conversion
```

### Pattern 3: Validation as Static Methods
```csharp
public sealed record Email
{
    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        EmailRegex.IsMatch(value);
    
    public static Email? TryCreate(string value)
    {
        try
        {
            return new Email(value);
        }
        catch
        {
            return null;
        }
    }
}
```

### Pattern 4: With Methods (Non-Destructive Mutation)
```csharp
public sealed record Address
{
    // ... properties
    
    public Address WithStreet(string newStreet) =>
        new Address(newStreet, City, State, PostalCode, CountryCode, Street2);
    
    public Address WithCity(string newCity) =>
        new Address(Street, newCity, State, PostalCode, CountryCode, Street2);
}

var address = new Address("123 Main", "Springfield", "IL", "62701", "US");
var updatedAddress = address.WithStreet("456 Oak"); // New instance
```

---

## Testing Value Objects

### Unit Test Examples

```csharp
[TestClass]
public class MoneyTests
{
    [TestMethod]
    public void Money_WithSameValues_ShouldBeEqual()
    {
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(10.50m, "USD");
        
        Assert.AreEqual(money1, money2);
    }
    
    [TestMethod]
    public void Money_Addition_ShouldReturnCorrectSum()
    {
        var money1 = new Money(10.00m, "USD");
        var money2 = new Money(5.50m, "USD");
        var expected = new Money(15.50m, "USD");
        
        var result = money1 + money2;
        
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Money_AdditionWithDifferentCurrencies_ShouldThrow()
    {
        var usd = new Money(10.00m, "USD");
        var eur = new Money(10.00m, "EUR");
        
        var result = usd + eur; // Should throw
    }
}

[TestClass]
public class EmailTests
{
    [TestMethod]
    public void Email_WithValidAddress_ShouldCreate()
    {
        var email = new Email("test@example.com");
        
        Assert.AreEqual("test@example.com", email.Value);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Email_WithInvalidAddress_ShouldThrow()
    {
        var email = new Email("not-an-email");
    }
    
    [TestMethod]
    public void Email_ShouldNormalizeValue()
    {
        var email = new Email("Test@Example.COM");
        
        Assert.AreEqual("test@example.com", email.NormalizedValue);
    }
}
```

---

## Best Practices Summary

✅ **DO:**
- Make value objects immutable
- Validate in the constructor
- Implement value equality (use `record` types)
- Include domain operations
- Use descriptive names
- Provide factory methods for common cases
- Test thoroughly

❌ **DON'T:**
- Add setters to properties
- Include identity (IDs)
- Make them entities
- Leave validation to consumers
- Use primitive types when a value object is appropriate
- Create anemic value objects (just data containers)

---

## Further Reading

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing Value Objects (Microsoft Docs)](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects)
- [Value Object Pattern (Martin Fowler)](https://martinfowler.com/bliki/ValueObject.html)
- [C# Records as Value Objects](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)

---

## Conclusion

Value Objects are a powerful tool for creating a rich, expressive domain model. By replacing primitive types with meaningful value objects, you:

- Encapsulate validation and business rules
- Improve type safety and reduce bugs
- Make your code more readable and maintainable
- Express domain concepts explicitly
- Create a ubiquitous language in code

Start identifying primitives in your entities that could be value objects, and gradually refactor them to create a more robust domain model.
