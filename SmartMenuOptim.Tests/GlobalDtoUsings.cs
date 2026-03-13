// Global DTO Type Aliases for SmartMenuOptim.Tests
// Provides access to Application DTOs with backward compatibility

// Restaurant feature DTOs
global using DishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishDTO;
global using CategoryDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.UnderperformingDishDTO;
global using MenuDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuDTO;
global using RestaurantDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDTO;

// Review/Sales DTOs
global using ReviewDTO = SmartMenuOptim.Application.Features.Reviews.DTOs.ReviewDTO;
global using SaleRecordDTO = SmartMenuOptim.Application.Features.Sales.DTOs.SaleRecordDTO;

// AI DTOs
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;
global using AiRecommendationResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationResponseDTO;

// Admin/Customer DTOs
global using AdminUserDTO = SmartMenuOptim.Application.Features.Admin.DTOs.AdminUserDTO;
global using CustomerDTO = SmartMenuOptim.Application.Features.Customers.DTOs.CustomerDTO;

// Legacy aliases (for old naming with typos)
global using AiRecomendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;
global using AiRecomendationResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationResponseDTO;
