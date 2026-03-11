// =============================================================================
// Global DTO Type Aliases for Backward Compatibility
// =============================================================================
// This file provides backward compatibility by creating global type aliases
// that allow existing code using SmartMenuOptim.Application.Dtos namespace
// to continue working while DTOs are organized into feature folders.
//
// Usage: Files can use either:
//   - Old: using SmartMenuOptim.Application.Dtos; then ReviewDTO
//   - New: using SmartMenuOptim.Application.Dtos.Review; then ReviewDTO
// =============================================================================

// Common DTOs
global using UserBaseDTO = SmartMenuOptim.Application.Dtos.Common.UserBaseDTO;

// Admin DTOs
global using AdminUserDTO = SmartMenuOptim.Application.Dtos.Admin.AdminUserDTO;
global using BusinessRuleDTO = SmartMenuOptim.Application.Dtos.Admin.BusinessRuleDTO;

// Customer DTOs
global using CustomerDTO = SmartMenuOptim.Application.Dtos.Customer.CustomerDTO;

// Dish DTOs
global using DishDTO = SmartMenuOptim.Application.Dtos.Dish.DishDTO;
global using CategoryDTO = SmartMenuOptim.Application.Dtos.Dish.CategoryDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Dtos.Dish.UnderperformingDishDTO;

// Review DTOs
global using ReviewDTO = SmartMenuOptim.Application.Dtos.Review.ReviewDTO;

// Sales DTOs
global using SaleRecordDTO = SmartMenuOptim.Application.Dtos.Sales.SaleRecordDTO;
global using CategoryGroupDTO = SmartMenuOptim.Application.Dtos.Sales.CategoryGroupDTO;

// Restaurant DTOs (moved to Features/Restaurants/DTOs)
global using RestaurantDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDTO;
global using RestaurantDetailDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDetailDTO;
global using RestaurantCreateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantCreateDTO;
global using RestaurantUpdateDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantUpdateDTO;
global using BusinessHoursDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.BusinessHoursDTO;
global using AddressDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.AddressDTO;

// Menu DTOs (remaining in Dtos/Restaurant, will move to Features/Menus later)
global using MenuDTO = SmartMenuOptim.Application.Dtos.Restaurant.MenuDTO;
global using MenuCreateDTO = SmartMenuOptim.Application.Dtos.Restaurant.MenuCreateDTO;
global using MenuUpdateDTO = SmartMenuOptim.Application.Dtos.Restaurant.MenuUpdateDTO;

// AI DTOs
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationRequestDTO;
global using AiRecommendationResponseDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationResponseDTO;
global using InsightResponseDTO = SmartMenuOptim.Application.Dtos.AI.InsightResponseDTO;

// Legacy aliases (for files using old naming with typos)
global using AiRecomendationRequestDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationRequestDTO;
global using AiRecomendationResponseDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationResponseDTO;
