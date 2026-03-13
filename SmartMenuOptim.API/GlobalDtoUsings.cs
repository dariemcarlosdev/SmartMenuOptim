// Global DTO Type Aliases for SmartMenuOptim.API
// Provides access to Application DTOs with backward compatibility in API layer.

global using SmartMenuOptim.Application.Features.Restaurants.DTOs;

// Type aliases for backward compatibility
global using DishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishDTO;
global using CategoryDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.UnderperformingDishDTO;
global using MenuDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuDTO;
global using ReviewDTO = SmartMenuOptim.Application.Features.Reviews.DTOs.ReviewDTO;
global using SaleRecordDTO = SmartMenuOptim.Application.Features.Sales.DTOs.SaleRecordDTO;
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;

// Legacy aliases (for old naming with typos)
global using AiRecomendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;
global using AiRecomendationResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationResponseDTO;
