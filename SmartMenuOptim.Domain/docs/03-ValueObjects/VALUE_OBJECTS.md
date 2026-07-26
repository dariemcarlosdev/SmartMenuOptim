# Value Objects - Domain-Driven Design

## Table of Contents
- [What are Value Objects?](#what-are-value-objects)
- [Characteristics of Value Objects](#characteristics-of-value-objects)
- [When Should You Create a Value Object?](#when-should-you-create-a-value-object)
- [DDD Best Practices](#ddd-best-practices)
- [Value Objects vs Entities](#value-objects-vs-entities)
- [Implementation in C# 12 / .NET 8](#implementation-in-c-12--net-8)
- [Value Objects in This Project](#value-objects-in-this-project)
- [Usage in Domain Entities](#usage-in-domain-entities)
- [Benefits](#benefits)
- [Common Patterns](#common-patterns)
- [Testing Value Objects](#testing-value-objects)
- [Best Practices Summary](#best-practices-summary)
- [Further Reading](#further-reading)
- [Conclusion](#conclusion)
- [Implementation Pattern for New Value Objects](#implementation-pattern-for-new-value-objects)

---

## What are Value Objects?

**Value Objects** are a way to represent important concepts in your business domain that don't need a unique identity. Think of them as "smart containers" that hold related data and behavior together.

### Simple Explanation

Imagine you're working with **money** in your application. Instead of using separate variables for amount and currency:

```csharp
// ❌ Primitive approach - easy to make mistakes
decimal amount = 25.99m;
string currency = "USD";
// What happens if you mix up the order? Pass amount where currency expected?
```

You create a **Value Object** that bundles them together with built-in rules:

```csharp
// ✅ Value Object approach - safe and expressive
Money price = new Money(25.99m, "USD");
// Can't mix up the parts, includes validation, has useful operations
```

### Real-World Analogy

Think of Value Objects like **coins or bills**:
- Two $20 bills are **exactly the same** - you don't care which specific bill you have
- What matters is the **value** ($20), not the individual bill's serial number
- If you lose one $20 bill and find another $20 bill, you have the same thing

This is different from something like your **driver's license**:
- Each license has a unique ID number
- You can't just swap your license with someone else's (even with same info)
- The **identity** matters, not just the values

### Key Concept
> **Value Objects** represent concepts that matter because of **what they are** (their values), not **which one they are** (their identity).

### Common Examples in Restaurant Apps

| Concept | Why it's a Value Object | What it Contains |
|---------|------------------------|------------------|
| **Email Address** | `"chef@restaurant.com"` = `"chef@restaurant.com"` | Validation rules, normalization |
| **Money/Price** | `$15.99 USD` = `$15.99 USD` | Amount, currency, calculations |
| **Phone Number** | `"(555) 123-4567"` = `"(555) 123-4567"` | Formatting, validation |
| **Address** | Same street + city + zip = Same address | Complete location info |
| **Rating** | 4 stars = 4 stars | Star value, percentage, descriptions |

---

## Characteristics of Value Objects

Value Objects follow 5 simple rules that make them powerful and safe to use:

### 1. **They Never Change (Immutability)**
Once you create a value object, you can't modify it. To "change" it, you create a new one.

**Why?** Just like you can't change a $20 bill into a $50 bill - you need a different bill.

```csharp
// ✅ CORRECT: Create new instances for changes
var originalPrice = new Money(10.50m, "USD");
var salePrice = new Money(8.50m, "USD"); // New instance for sale price

// ❌ IMPOSSIBLE: Can't modify existing value objects
// originalPrice.Amount = 8.50m; // No setter exists!
```

**Real benefit:** Prevents accidental changes that could break your app.

### 2. **Same Values = Same Object (Value Equality)**
Two value objects with identical content are considered exactly the same.

**Think:** Two identical business cards represent the same contact information.

```csharp
var price1 = new Money(10.50m, "USD");
var price2 = new Money(10.50m, "USD");
var price3 = new Money(10.50m, "EUR");

Console.WriteLine(price1 == price2); // ✅ TRUE - same amount and currency
Console.WriteLine(price1 == price3); // ❌ FALSE - different currency

// This is automatic with value objects - no need to write comparison logic!
```

### 3. **They Validate Themselves (Self-Validation)**
Value objects check their own data when created, so invalid objects can't exist.

**Think:** Like a smart form that won't submit with invalid data.

```csharp
// ✅ Valid email creates successfully
var validEmail = new Email("chef@restaurant.com");

// ❌ Invalid email throws error immediately - can't create broken objects
try 
{
    var invalidEmail = new Email("not-an-email");
}
catch (ArgumentException ex)
{
    Console.WriteLine("Caught invalid email!"); // Will execute
}

// This means: If you have an Email object, you KNOW it's valid!
```

### 4. **No Unique Identity (No ID Required)**
Value objects don't need ID numbers because their values identify them.

**Think:** You don't need a serial number on each "medium coffee order" - the description is enough.

```csharp
// Value objects are identified by their content
var address1 = new Address("123 Main St", "Springfield", "IL", "62701");
var address2 = new Address("123 Main St", "Springfield", "IL", "62701");

// No ID needed - if all parts match, they represent the same address
Console.WriteLine(address1 == address2); // ✅ TRUE

// Compare to entities that NEED IDs:
var customer1 = new Customer { Id = 1, Name = "John" };
var customer2 = new Customer { Id = 2, Name = "John" };
// Same name, different people! IDs make them unique.
```

### 5. **Complete Concepts (Not Just Data Containers)**
Value objects represent whole business concepts with behavior, not just data storage.

**Think:** A "Money" object doesn't just hold numbers - it knows how to add, subtract, format, etc.

```csharp
// ❌ Primitive obsession - just data, no behavior
decimal amount = 10.50m;
string currency = "USD";
// How do you add? Convert currency? Format for display? Code scattered everywhere!

// ✅ Rich value object - data + behavior in one place
Money price = new Money(10.50m, "USD");
Money tax = new Money(0.84m, "USD");

Money total = price + tax;                    // Addition built-in
string formatted = price.ToString("$0.00");  // Formatting built-in  
bool isPositive = price.IsPositive;           // Business logic built-in

// All money-related logic lives with the Money concept!
```

### Quick Mental Model

Think of Value Objects like **LEGO blocks**:
- **Immutable**: You can't bend a LEGO block into a different shape
- **Value Equality**: Two identical red 2x4 blocks are the same
- **Self-Validating**: LEGO blocks only connect in valid ways
- **No Identity**: You don't care which specific red block you grab
- **Complete Concept**: A "red 2x4 block" is a complete, useful thing

This makes your code like a well-designed LEGO set - all pieces fit together correctly!

---

## When Should You Create a Value Object?

### The "Primitive Smell" Test

If you find yourself writing code like this, you probably need a Value Object:

```csharp
// 🚨 RED FLAGS - Primitive Obsession Smells
public class MenuItem
{
    public decimal Price { get; set; }
    public string Currency { get; set; }          // Always goes with Price
    public string Email { get; set; }             // Needs validation 
    public int Rating { get; set; }               // Must be 1-5, but int allows 999
    public string Phone { get; set; }             // Needs formatting
    
    // Validation scattered everywhere!
    public bool IsValidEmail() => /* regex here */;
    public bool IsValidRating() => Rating >= 1 && Rating <= 5;
    public string FormatPrice() => $"{Price:C} {Currency}";
}
```

### The Value Object Solution

```csharp
// ✅ CLEAN - Using Value Objects  
public class MenuItem
{
    public Money Price { get; set; }              // Handles amount + currency + formatting
    public Email ContactEmail { get; set; }       // Handles validation + normalization
    public Rating CustomerRating { get; set; }    // Handles 1-5 range + descriptions
    public PhoneNumber Phone { get; set; }        // Handles formatting + validation
    
    // No validation methods needed - value objects handle it!
}
```

### Simple Decision Tree

Ask yourself these questions:

1. **"Does this data need validation?"** 
   - Email format ✅ → Value Object
   - Person's age ✅ → Value Object  
   - Random text ❌ → Keep as string

2. **"Do these pieces of data always go together?"**
   - Amount + Currency ✅ → Money Value Object
   - Street + City + Zip ✅ → Address Value Object
   - Separate unrelated fields ❌ → Keep separate

3. **"Do I keep writing the same validation/formatting code?"**
   - Email validation everywhere ✅ → Email Value Object
   - Phone formatting in multiple places ✅ → PhoneNumber Value Object
   - One-off validation ❌ → Maybe just a property

4. **"Is this a concept my business users understand?"**
   - "Price", "Email", "Address" ✅ → Great Value Objects
   - "Configuration flag XYZ" ❌ → Technical detail, not domain concept

### Common Value Object Candidates

| If you have this... | Consider this Value Object | ✅ Implemented |
|---------------------|---------------------------|-------------|
| `decimal amount, string currency` | `Money` | ✅ |
| `string email` (with validation) | `Email` | ✅ |
| `string phone` (with formatting) | `PhoneNumber` | ✅ |
| `int rating` (must be 1-5) | `Rating` | ✅ Recently Added |
| `string street, city, state, zip` | `Address` | ✅ |
| `decimal percent` (0.0-1.0) | `Percentage` | ✅ |
| `string name` (with business rules) | `DishName`, `CustomerName` | ✅ `DishName` Recently Added |
| `DateTime start, DateTime end` | `TimeRange`, `BusinessHours` | 🔄 Planned |
| `string description` (with length rules) | `Description`, `Notes` | 🔄 Consider |
| `string code` (with format rules) | `ProductCode`, `DiscountCode` | 🔄 Consider |

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

Understanding the difference is crucial for good domain design:

### The Golden Question: "Is this thing unique, or just valuable?"

**Value Objects** → **"What matters is WHAT it is"**
- Two $10 bills are the same
- Two email addresses "chef@place.com" are identical
- Two 5-star ratings mean exactly the same thing

**Entities** → **"What matters is WHICH ONE it is"**
- Two customers named "John Smith" are different people
- Two restaurants at the same address are different businesses
- Two orders for $25 are separate transactions

### Quick Comparison

| Aspect | Value Object | Entity | Example |
|--------|-------------|--------|---------|
| **What makes it unique?** | Its values | Its ID | Money vs Customer |
| **Can you swap identical ones?** | ✅ Yes | ❌ No | Two $5 bills vs Two people |
| **Does it change?** | ❌ Create new one | ✅ Update properties | New price vs Update customer info |
| **Identity over time?** | No identity | Same person/thing | Email stays same meaning vs Person grows older |

### Real Examples from Our Restaurant App

```csharp
// 🏷️ VALUE OBJECTS - Defined by their content
var restaurantEmail = new Email("contact@restaurant.com");
var menuItemPrice = new Money(12.99m, "USD"); 
var customerRating = new Rating(5); // 5 stars
var deliveryAddress = new Address("123 Main St", "Springfield", "IL");

// If you create the same values again, they're identical:
var sameEmail = new Email("contact@restaurant.com");
// restaurantEmail == sameEmail ✅ TRUE

// 🆔 ENTITIES - Defined by their unique identity  
var customer1 = new Customer { Id = 1, Name = "John", Email = restaurantEmail };
var customer2 = new Customer { Id = 2, Name = "John", Email = restaurantEmail };

// Even with same name and email, they're different people:
// customer1 == customer2 ❌ FALSE (different IDs)

// 🔄 HOW THEY CHANGE
// Value Object: Create a new one
var newPrice = new Money(15.99m, "USD"); // Create new
menuItem.Price = newPrice; // Replace entire object

// Entity: Modify the existing one
customer1.Name = "Johnny"; // Update property
customer1.Email = new Email("johnny@email.com"); // Replace value object property
// customer1 is still the same customer (same ID), just updated
```

### Memory Aid: The "Business Card" Test

Think of **Value Objects** like information **ON** a business card:
- Email, phone, address are value objects
- If two cards have identical info, they represent the same contact details
- You don't care which physical card you have

Think of **Entities** like the **PERSON** the business card represents:
- Each person is unique, regardless of their contact info
- Two people can have identical business cards but still be different people
- The person has an identity beyond their contact details

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

This project includes the following value objects, all following a consistent implementation pattern:

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

### 6. **DishName** ⭐ *Recently Added*
```csharp
var dishName = new DishName("Margherita Pizza");

// Access properties
string display = dishName.Value;           // "Margherita Pizza"
string normalized = dishName.NormalizedValue; // "margherita pizza"
string searchable = dishName.SearchValue;     // "margherita pizza"

// Utility methods
bool isSpecial = dishName.IsSpecialtyDish(); // false
string abbreviated = dishName.GetAbbreviated(15); // "Margherita..."
```

**Features:**
- Validates dish names (3-100 characters, valid characters only)
- Provides normalized and search-friendly versions
- Includes utility methods like `IsSpecialtyDish()` and `GetAbbreviated()`
- Immutable record with implicit conversion to string

**Usage:**
- Menu item names
- Recipe titles
- Search functionality
- Display formatting

### 7. **Rating** ⭐ *Recently Added*
```csharp
var rating = new Rating(4);

// Access properties
int value = rating.Value;           // 4
string desc = rating.Description;   // "Very Good"
bool positive = rating.IsPositive;  // true
double percentage = rating.Percentage; // 0.75

// Conversion methods
var fromPercent = Rating.FromPercentage(0.8); // 4 stars
var fromDecimal = Rating.FromDecimal(4.2);    // 4 stars

// Visualization
string stars = rating.ToStarString(); // "★★★★☆"

// Calculations
var averageRating = Rating.CalculateAverage(reviewRatings);
```

**Features:**
- Enforces 1-5 star rating range
- Provides utility properties (`IsPositive`, `IsNegative`, `IsNeutral`)
- Includes conversion methods (`FromPercentage`, `FromDecimal`)
- Star visualization with `ToStarString()` method
- Static methods for calculating averages
- Comparison operators

**Usage:**
- Customer reviews and feedback
- Menu item ratings
- Service quality scores
- Performance metrics

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

### Example 2: MenuItem/Dish Entity

```csharp
public class Dish : EntityBase
{
    public int Id { get; set; }
    
    // Value Objects for naming and pricing
    public DishName Name { get; set; }
    public string? Description { get; set; }
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
    
    // Domain operations with value objects
    public void UpdateName(string newName)
    {
        Name = new DishName(newName); // Automatic validation
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ApplyDiscount(Percentage discount)
    {
        if (discount.Value > 0.5m) // Max 50% discount
            throw new InvalidOperationException("Discount cannot exceed 50%");
        
        Discount = discount;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Search-friendly methods
    public bool MatchesSearch(string searchTerm)
    {
        return Name.SearchValue.Contains(searchTerm.ToLowerInvariant()) ||
               Name.NormalizedValue.Contains(searchTerm.ToLowerInvariant());
    }
    
    public bool IsSpecialtyItem() => Name.IsSpecialtyDish();
}
```

### Example 2.1: Review Entity with Rating Value Object

```csharp
public class Review : EntityBase
{
    public int Id { get; set; }
    public int DishId { get; set; }
    public int? CustomerId { get; set; }
    
    // Value Objects for rating and feedback
    public Rating Rating { get; private set; }
    public string Comment { get; private set; }
    public double SentimentScore { get; private set; }
    public string CustomerName { get; private set; }
    
    // Constructor with Rating value object
    public Review(
        int restaurantId,
        int dishId,
        int rating,
        string comment,
        int customerId,
        double sentimentScore = 0.5)
    {
        // ... validation
        Rating = new Rating(rating); // Automatic validation
        Comment = comment?.Trim() ?? string.Empty;
        // ... other properties
    }
    
    // Update methods using value objects
    public void UpdateReview(int rating, string comment)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted review.");

        Rating = new Rating(rating); // Type-safe validation
        Comment = comment?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Domain queries using Rating behavior
    public bool IsPositiveReview() => Rating.IsPositive;
    public bool IsNegativeReview() => Rating.IsNegative;
    public string GetStarDisplay() => Rating.ToStarString();
    public string GetRatingDescription() => Rating.Description;
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

---

## Implementation Pattern for New Value Objects

**⚠️ IMPORTANT: When adding new value objects to this project, follow this established pattern to maintain consistency and quality.**

### 1. **Value Object Implementation Checklist**

✅ **Core Structure:**
- Use `sealed record` for immutability and value equality
- Include parameterless constructor for EF Core
- Add comprehensive validation in primary constructor
- Implement `ToString()` and implicit conversions where appropriate

✅ **Properties:**
- `Value` property for the main value
- `NormalizedValue` property for search/comparison (if applicable)
- Additional computed properties for domain behavior

✅ **Methods:**
- Domain-specific utility methods
- Static factory methods for common scenarios
- Validation helper methods (static or instance)

✅ **Validation:**
- Validate in constructor with descriptive error messages
- Use constants for validation limits
- Include business rules and constraints

### 2. **Standard Implementation Template**

```csharp
using System.Text.RegularExpressions;

namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a [concept name] value object with validation and [specific behavior].
/// </summary>
/// <remarks>
/// This value object ensures that [concept] are always valid, normalized, and conform to business rules.
/// It is immutable and defined by its value rather than identity.
/// [Additional behavior description]
/// </remarks>
public sealed record YourValueObject
{
    // Constants for validation
    public const int MinLength = X;
    public const int MaxLength = Y;
    
    // Static validation patterns (if needed)
    private static readonly Regex ValidationRegex = new(
        @"[your-pattern]",
        RegexOptions.Compiled);

    /// <summary>
    /// Gets the primary value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Gets the normalized version for searching/comparison.
    /// </summary>
    public string NormalizedValue { get; init; }

    /// <summary>
    /// Additional computed properties for domain behavior.
    /// </summary>
    public bool SomeDomainProperty => /* logic */;

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private YourValueObject()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YourValueObject"/> class.
    /// </summary>
    /// <param name="Value">The value to validate and store.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
    public YourValueObject(string Value)
    {
        // Standard validation pattern
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("[concept] cannot be empty.", nameof(Value));

        if (Value.Length < MinLength)
            throw new ArgumentException($"[concept] must be at least {MinLength} characters long.", nameof(Value));

        if (Value.Length > MaxLength)
            throw new ArgumentException($"[concept] cannot exceed {MaxLength} characters.", nameof(Value));

        var trimmedValue = Value.Trim();
        
        // Additional validation (format, business rules, etc.)
        if (!ValidationRegex.IsMatch(trimmedValue))
            throw new ArgumentException("Invalid [concept] format.", nameof(Value));

        // Set properties
        this.Value = trimmedValue;
        NormalizedValue = trimmedValue.ToLowerInvariant();
    }

    /// <summary>
    /// Domain-specific behavior method.
    /// </summary>
    public bool SomeDomainMethod()
    {
        // Implementation
        return true;
    }

    /// <summary>
    /// Static factory method for common scenarios.
    /// </summary>
    public static YourValueObject FromSomething(string input)
    {
        // Transform and create
        return new YourValueObject(input);
    }

    public override string ToString() => Value;

    public static implicit operator string(YourValueObject valueObject) => valueObject.Value;

    public static explicit operator YourValueObject(string value) => new(value);
}
```

### 3. **EF Core Value Converter Template**

Create a corresponding value converter:

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Provides a value converter for mapping <see cref="YourValueObject"/> objects to their string representations and vice versa
/// for use with Entity Framework Core.
/// </summary>
public sealed class YourValueObjectValueConverter : ValueConverter<YourValueObject, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YourValueObjectValueConverter"/> class.
    /// </summary>
    public YourValueObjectValueConverter() 
        : base(
            // Convert to database type
            valueObject => valueObject != null ? valueObject.Value : null,
            // Convert from database type
            value => value != null ? new YourValueObject(value) : null)
    {
    }
}
```

### 4. **AppDbContext Configuration Template**

Add configuration method in `AppDbContext`:

```csharp
/// <summary>
/// Configures value conversion for YourValueObject across all entities.
/// </summary>
private void ConfigureYourValueObjectConversion(ModelBuilder modelBuilder)
{
    var converter = new YourValueObjectValueConverter();

    var properties = modelBuilder.Model.GetEntityTypes()
        .SelectMany(t => t.GetProperties())
        .Where(p => p.ClrType == typeof(YourValueObject));

    foreach (var property in properties)
    {
        property.SetValueConverter(converter);
        property.SetMaxLength(YourValueObject.MaxLength); // Match validation constraint
    }
}
```

Don't forget to call it in `OnModelCreating()`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations
    ConfigureYourValueObjectConversion(modelBuilder);
}
```

### 5. **Testing Template**

Create comprehensive unit tests:

```csharp
[TestClass]
public class YourValueObjectTests
{
    [TestMethod]
    public void Constructor_WithValidValue_ShouldCreate()
    {
        var valueObject = new YourValueObject("valid-value");
        
        Assert.AreEqual("valid-value", valueObject.Value);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithInvalidValue_ShouldThrow()
    {
        var valueObject = new YourValueObject("invalid");
    }
    
    [TestMethod]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var value1 = new YourValueObject("test");
        var value2 = new YourValueObject("test");
        
        Assert.AreEqual(value1, value2);
    }
    
    // Add tests for all domain methods and edge cases
}
```

### 6. **Integration Steps**

When implementing a new value object:

1. ✅ **Create the value object** following the template
2. ✅ **Create the value converter** for EF Core
3. ✅ **Update AppDbContext** configuration
4. ✅ **Update entities** to use the value object
5. ✅ **Update specifications** and queries if needed
6. ✅ **Update seeders** to use the value object
7. ✅ **Write unit tests** for validation and behavior
8. ✅ **Update documentation** with new examples

### 7. **Real Examples to Follow**

**Study these implemented Value Objects for consistency:**
- **`DishName`** - String validation with search optimization
- **`Rating`** - Integer validation with domain behavior  
- **`Email`** - Format validation with normalization
- **`Money`** - Multi-property object with operations
- **`Address`** - Complex object with formatting

### 8. **Implementation Success Stories**

**✅ Recently Implemented:**

**`DishName` Value Object:**
- **Before:** `public string Name { get; set; }` (primitive obsession)
- **After:** `public DishName Name { get; set; }` (rich domain model)
- **Benefits:** Automatic validation, search optimization, specialty dish detection
- **Database Impact:** None (seamless value conversion)

**`Rating` Value Object:**
- **Before:** `public int Rating { get; set; }` (no validation)
- **After:** `public Rating Rating { get; set; }` (type-safe, rich behavior)
- **Benefits:** Guaranteed 1-5 range, star visualization, utility methods
- **Database Impact:** None (seamless value conversion)

**Follow this same pattern for all new value objects to maintain consistency and quality across the domain model!**
