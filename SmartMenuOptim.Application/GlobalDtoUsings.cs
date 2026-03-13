// =============================================================================
// Global DTO Type Aliases for Backward Compatibility
// =============================================================================
// This file provides backward compatibility by creating global type aliases
// that allow existing code using old namespace paths to continue working
// while DTOs are organized into feature folders.
// =============================================================================

// Common DTOs (shared base — stays in Dtos/Common/)
global using UserBaseDTO = SmartMenuOptim.Application.Dtos.Common.UserBaseDTO;

// Admin DTOs (migrated to Features/Admin/DTOs/)
global using AdminUserDTO = SmartMenuOptim.Application.Features.Admin.DTOs.AdminUserDTO;
global using BusinessRuleDTO = SmartMenuOptim.Application.Features.Admin.DTOs.BusinessRuleDTO;

// Customer DTOs (migrated to Features/Customers/DTOs/)
global using CustomerDTO = SmartMenuOptim.Application.Features.Customers.DTOs.CustomerDTO;

// Dish/Category DTOs (migrated to Features/Restaurants/DTOs/)
global using DishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishDTO;
global using DishCreateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishCreateDTO;
global using DishUpdateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishUpdateDTO;
global using CategoryDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryDTO;
global using CategoryCreateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryCreateDTO;
global using CategoryUpdateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryUpdateDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.UnderperformingDishDTO;

// Menu DTOs (migrated to Features/Restaurants/DTOs/)
global using MenuDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuDTO;
global using MenuCreateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuCreateDTO;
global using MenuUpdateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuUpdateDTO;

// Review DTOs (already in Features/Reviews/DTOs/)
global using ReviewDTO = SmartMenuOptim.Application.Features.Reviews.DTOs.ReviewDTO;

// Sales DTOs (already in Features/Sales/DTOs/)
global using SaleRecordDTO = SmartMenuOptim.Application.Features.Sales.DTOs.SaleRecordDTO;
global using CategoryGroupDTO = SmartMenuOptim.Application.Features.Sales.DTOs.CategoryGroupDTO;

// Restaurant DTOs (moved to Features/Restaurants/DTOs)
global using RestaurantDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDTO;
global using RestaurantDetailDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDetailDTO;
global using RestaurantCreateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantCreateDTO;
global using RestaurantUpdateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantUpdateDTO;
global using BusinessHoursDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.BusinessHoursDTO;
global using AddressDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.AddressDTO;

// AI DTOs (already in Features/AI/DTOs/)
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;
global using AiRecommendationResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationResponseDTO;
global using InsightResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.InsightResponseDTO;

// AI Contracts (already in Features/AI/Contracts/)
global using IAImprovementStrategyService = SmartMenuOptim.Application.Features.AI.Contracts.IAImprovementStrategyService;
