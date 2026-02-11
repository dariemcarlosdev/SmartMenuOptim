# Security Documentation

## 📚 Overview

Complete security implementation guide for **SmartMenuOptim** - a multi-tenant restaurant management platform built with .NET 8/9 and Blazor Server.

**Architecture:** Each **Restaurant** is an isolated tenant with database-level query filters and application-level validation.

---

## 📂 Document Relationship

### **Two Documents, One Goal:**

| Document | Type | Size | Purpose | Read When |
|----------|------|------|---------|-----------|
| **Security-Guidelines-Multi-Tenancy.md** | Master Guide | 33.5 KB | Complete end-to-end security implementation | ✅ Always start here |
| **Permission-System-Design.md** | Deep Dive | 15 KB | Permission system rationale & design | 🔐 Implementing permissions |

### **Quick Summary:**

- **Security-Guidelines** = Complete car manual (covers everything)
- **Permission-System-Design** = Deep-dive on the transmission (one system in detail)
- **Overlap:** Only 15% (Section 4.2) - the rest is unique content
- **Reading Order:** Master doc first, permission doc when needed

---

## 🔍 How These Documents Work Together

### **Visual Structure:**

```
┌──────────────────────────────────────────────────────────────┐
│  Security-Guidelines-Multi-Tenancy.md (MASTER GUIDE)        │
│  ══════════════════════════════════════════════════════════  │
│                                                              │
│  Section 1:  Implementation Status & Action Plan            │
│  Section 2:  Architecture Overview                          │
│  Section 3:  Data Access & Repository Pattern               │
│  Section 4:  Authentication & Authorization                 │
│              ├─ 4.1: Identity Integration                   │
│              ├─ 4.2: Permission System ──────────────┐      │
│              └─ 4.3: Authorization Handler           │      │
│  Section 5:  API Security Controls                   │      │
│  Section 6:  Azure Security Integration              │      │
│  Section 7:  Monitoring & Audit Logging              │      │
│  Section 8:  Blazor Security (comprehensive)         │      │
│  Section 9:  Testing & Validation                    │      │
│  Section 10: Operations & Maintenance                │      │
│  Section 11: Compliance & Standards                  │      │
│                                                       │      │
└───────────────────────────────────────────────────────┘      │
                                                               │
                    References Section 4.2                     │
                             │                                 │
                             ▼                                 │
┌──────────────────────────────────────────────────────────────┘
│
│  ┌────────────────────────────────────────────────────────┐
└─▶│  Permission-System-Design.md (DEEP DIVE REFERENCE)    │
   │  ════════════════════════════════════════════════════  │
   │                                                         │
   │  1. Identity vs Application Permissions                │
   │  2. Multi-tenant Permission Requirements               │
   │  3. Profile-Specific Permissions                       │
   │  4. Flexible Permission Assignment                     │
   │  5. Profile-Specific Access Control                    │
   │  6. Business Rule Integration                          │
   │  7. Implementation Benefits                            │
   │  8. Best Practices (Caching, Cleanup)                  │
   │                                                         │
   └─────────────────────────────────────────────────────────┘
```

**Cross-Reference:** Section 4.2 of the master doc links to Permission-System-Design.md for detailed permission implementation.

---

## 📊 Content Overlap Analysis

| Topic | Master Guide | Permission Doc | Overlap % |
|-------|-------------|----------------|-----------|
| **Implementation Status** | ✅ Complete | ❌ None | 0% |
| **Tenant Isolation** | ✅ Complete | ❌ None | 0% |
| **Data Access** | ✅ Complete | ❌ None | 0% |
| **Permission System** | ✅ Overview | ✅ Deep-dive | **15%** |
| **API Security** | ✅ Complete | ❌ None | 0% |
| **Azure Integration** | ✅ Complete | ❌ None | 0% |
| **Blazor Security** | ✅ Complete | ✅ Brief example | 5% |
| **Monitoring** | ✅ Complete | ❌ None | 0% |
| **Business Rules** | ❌ None | ✅ Complete | 0% |
| **Permission Caching** | ❌ None | ✅ Complete | 0% |
| **Testing** | ✅ Complete | ❌ None | 0% |
| **Operations** | ✅ Complete | ❌ None | 0% |

**Result:** Only 15% overlap (Section 4.2). **85% unique content** in each document.

---

## 🎯 Quick Decision Guide

### **When to Use Each Document:**

| Your Question | Read This | Time | Section |
|---------------|-----------|------|---------|
| "How do I secure my app?" | Master Doc - All sections | 2.5 hrs | 1-11 |
| "How do I implement tenant isolation?" | Master Doc | 35 min | 2-3 |
| "How do I secure Blazor components?" | Master Doc | 30 min | 8 |
| "Why use custom permissions vs roles?" | Permission Doc | 30 min | All |
| "How do I add time-based permissions?" | Permission Doc | 10 min | 4 |
| "How do I optimize permission checks?" | Permission Doc | 15 min | Best Practices |
| "How do I set up Azure security?" | Master Doc | 20 min | 6 |
| "How do I add business rules to permissions?" | Permission Doc | 15 min | 6 |
| "How do I test tenant isolation?" | Master Doc | 15 min | 9 |
| "How do I implement permission cleanup?" | Permission Doc | 10 min | Best Practices |

### **Use Security-Guidelines-Multi-Tenancy.md When:**

✅ You're new to the project  
✅ Implementing any security feature  
✅ Need tenant isolation guidance  
✅ Setting up Blazor authentication  
✅ Deploying to Azure  
✅ Need implementation checklists  
✅ Want testing examples  
✅ Setting up monitoring  

### **Use Permission-System-Design.md When:**

🔐 Understanding WHY we use custom permissions  
🔐 Implementing the permission system  
🔐 Adding business rule automation  
🔐 Optimizing permission performance  
🔐 Setting up permission expiration  
🔐 Making architectural decisions about permissions  
🔐 Need to explain the design to stakeholders  

---

## 📖 Reading Paths by Scenario

### **Path 1: Complete Understanding (Recommended)**
1. **Security-Guidelines-Multi-Tenancy.md** - All sections **(2.5 hours)**
2. **Permission-System-Design.md** - Complete read **(30 minutes)**
3. **Total Time:** 3 hours

**Result:** Expert-level understanding of all security aspects

### **Path 2: Implementation Focus (Backend)**
1. **Security-Guidelines-Multi-Tenancy.md** - Sections 1-5 **(1.5 hours)**
2. **Permission-System-Design.md** - Complete read **(30 minutes)**
3. **Security-Guidelines-Multi-Tenancy.md** - Section 9 **(15 minutes)**
4. **Total Time:** 2 hours

**Result:** Ready to implement backend security

### **Path 3: Blazor Frontend Focus**
1. **Security-Guidelines-Multi-Tenancy.md** - Section 1 **(15 minutes)**
2. **Security-Guidelines-Multi-Tenancy.md** - Section 8 **(30 minutes)**
3. **Permission-System-Design.md** - Blazor integration **(10 minutes)**
4. **Total Time:** 55 minutes

**Result:** Ready to secure Blazor components

### **Path 4: DevOps/Azure Focus**
1. **Security-Guidelines-Multi-Tenancy.md** - Section 1 **(15 minutes)**
2. **Security-Guidelines-Multi-Tenancy.md** - Section 6 **(20 minutes)**
3. **Security-Guidelines-Multi-Tenancy.md** - Section 10 **(10 minutes)**
4. **Total Time:** 45 minutes

**Result:** Ready to deploy securely to Azure

---

## ❓ Frequently Asked Questions

### **Q: Can I read just one document?**

**A:** Depends on your goal:
- **Just Security-Guidelines:** You'll know HOW to implement but miss the WHY behind permission decisions
- **Just Permission-System-Design:** You'll understand permissions deeply but miss 85% of security (tenant isolation, Blazor, Azure, etc.)
- **Both:** Complete understanding **(recommended)**

### **Q: Why are they separate documents?**

**A:** Three reasons:
1. **Clarity:** Keeps the master doc focused on implementation, not design philosophy
2. **Audience:** Permission doc can be shared with architects/stakeholders independently
3. **Maintainability:** Permission design decisions can evolve without updating the entire security guide

### **Q: Which do I read first?**

**A:** Always start with **Security-Guidelines-Multi-Tenancy.md**. It's your primary guide and will tell you when to reference the permission doc (Section 4.2).

### **Q: Do I need to read both?**

**A:** 
- **If implementing permissions:** ✅ YES (both required)
- **If implementing other security (Blazor, Azure, testing):** Security-Guidelines only
- **If architecting the system:** ✅ YES (both required)
- **If onboarding to the project:** ✅ YES (both recommended)

### **Q: What if I only care about Blazor security?**

**A:** Follow **Path 3** above - you'll need Section 8 from the master doc primarily, with brief references to the permission doc for `AuthorizeView` patterns.

---

## 🔄 Document Consolidation History

### **Version 3.0 (January 2025) - Current**
- **Before:** 4 separate documents with 40% duplication
- **After:** 2 documents with 15% overlap
- **Result:** Single source of truth with clear separation of concerns

### **Obsolete Files Removed:**
- 🗑️ `permission-system.md` (duplicate of Permission-System-Design.md)
- 🗑️ `Data-Access-Security-Guidelines.md` (merged into Security-Guidelines Section 3)
- 🗑️ `Blazor-security-guidelines.md` (merged into Security-Guidelines Section 8)

### **Benefits:**
- ✅ Zero duplicate content
- ✅ Clear documentation hierarchy
- ✅ Easier to maintain and update
- ✅ Better navigation for all roles

---

## 🚀 Quick Start

### **New to the Project?**
Start here: [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) **Section 1**  
⏱️ **15 minutes** | Learn current status, risks, and immediate action items

### **Implementing Backend Security?**
Read: [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) **Sections 2-5**  
⏱️ **45 minutes** | Architecture, data access, authentication, API security

### **Implementing Blazor Frontend Security?**
Read: [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) **Section 8**  
⏱️ **30 minutes** | Blazor authentication, AuthorizeView patterns, claims-based UI

### **Deep Dive into Permissions?**
Read: [`Permission-System-Design.md`](Permission-System-Design.md)  
⏱️ **30 minutes** | Custom permission system, profile-specific access, business rules

### **Deploying to Azure?**
Read: [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) **Section 6**  
⏱️ **20 minutes** | Key Vault, Managed Identity, PostgreSQL security

---

## 📖 Document Structure

### **Master Document (All-in-One)**
**[`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md)** - Complete security implementation guide

| Section | Topic | Time |
|---------|-------|------|
| **1** | Implementation Status & Action Plan | 15 min |
| **2** | Architecture Overview | 20 min |
| **3** | Data Access & Repository Pattern | 15 min |
| **4** | Authentication & Authorization | 20 min |
| **5** | API Security Controls | 10 min |
| **6** | Azure Security Integration | 20 min |
| **7** | Monitoring & Audit Logging | 10 min |
| **8** | **Blazor Security Implementation** | 30 min |
| **9** | Testing & Validation | 15 min |
| **10** | Operations & Maintenance | 10 min |
| **11** | Compliance & Standards | 10 min |

### **Deep Dive Document**
**[`Permission-System-Design.md`](Permission-System-Design.md)** - Custom permission system details

- Why custom permissions vs. roles?
- Multi-tenant permission scoping
- Profile-specific access control
- Business rule integration
- Audit trails and caching

---

## 👥 Reading Paths by Role

### **Backend Developer**
1. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Sections 1-5
2. [`Permission-System-Design.md`](Permission-System-Design.md)
3. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 9 (Testing)

### **Frontend Developer (Blazor)**
1. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 1 (Status)
2. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 8 (Blazor Security)
3. [`Permission-System-Design.md`](Permission-System-Design.md) - Section on Blazor integration

### **DevOps Engineer**
1. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 1 (Action Plan)
2. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 6 (Azure Integration)
3. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 10 (Operations)

### **Security Auditor**
1. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 1 (Risks)
2. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 4 (Auth)
3. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 9 (Testing)
4. [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 11 (Compliance)

---

## 🔗 Related Documentation

- **Database Security:** `../database/migrations/MIGRATION GUIDE.md`
- **Azure Deployment:** `../deployment/AZURE-SETUP-GUIDE.md`
- **Project README:** `../README.md`

---

## ⚠️ Critical Security Notes

### **🔴 Not Yet Implemented (Priority P0):**
- 🔴 `TenantResolverMiddleware` not registered in `Program.cs`
- 🔴 `TenantAuthorizationHandler` not created
- 🔴 EF Core tenant query filters disabled

### **See Action Plan:** [`Security-Guidelines-Multi-Tenancy.md`](Security-Guidelines-Multi-Tenancy.md) - Section 1.3

---

## 📊 Implementation Progress

**Current Status:** 42% Complete  
**Priority:** ⚠️ Complete Phase 1 (6-8 hours) before production deployment

| Phase | Status | Time Estimate |
|-------|--------|---------------|
| **Phase 1 (Critical)** | 🔴 In Progress | 6-8 hrs |
| **Phase 2 (Testing)** | ⏳ Pending | 3-4 hrs |
| **Phase 3 (Azure)** | ⏳ Pending | 4-6 hrs |

---

## 🛠️ Quick Reference

### **Key Files Mentioned in Documentation:**
- `SmartMenuOptim.Shared/Data/Context/AppDbContext.cs` - Database context with query filters
- `SmartMenuOptim.Infrastructure/Middlewares/TenantResolverMiddleware.cs` - Tenant resolution
- `SmartMenuOptim.API/Extensions/ServiceCollectionExtensions.cs` - Service registration
- `SmartMenuOptim.Server/Extensions/ClaimsPrincipalExtensions.cs` - Claims helpers (to be created)

### **Important Concepts:**
- **Tenant:** Each `Restaurant` is a separate tenant
- **Tenant Identifier:** `RestaurantId` (integer)
- **Isolation Strategy:** Database query filters + application-level validation
- **Permission Model:** Custom fine-grained permissions beyond role-based access

---

## 📝 Document Changelog

### **Version 3.0 (January 2025)**
- ✅ Consolidated 4 separate documents into unified guide
- ✅ Added Section 8: Blazor Security Implementation
- ✅ Enhanced Section 3: Data Access & Repository patterns
- 🗑️ Removed duplicate `permission-system.md`
- 🗑️ Merged `Data-Access-Security-Guidelines.md` content
- 🗑️ Merged `Blazor-security-guidelines.md` content

### **Version 2.1 (December 2024)**
- Added Implementation Status section
- Reorganized document structure for better flow

---

## 💬 Need Help?

- **Technical Questions:** Code comments in implementation files + Section 8.9 checklists
- **Deployment Issues:** Azure Setup Guide (`../deployment/AZURE-SETUP-GUIDE.md`)
- **Security Concerns:** Section 10.1 (Incident Response) in master doc
- **Confused about which doc?** Use the "Quick Decision Guide" above ⬆️

---

**Last Updated:** January 2025  
**Maintained By:** SmartMenuOptim Security Team  
**Git Branch:** `env-dev/feature/authoritation-implement`
