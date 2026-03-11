// Global DTO Type Aliases for SmartMenuOptim.API
// Provides access to Application DTOs with backward compatibility

global using SmartMenuOptim.Application.Dtos;
global using SmartMenuOptim.Application.Dtos.AI;
global using SmartMenuOptim.Application.Dtos.Admin;
global using SmartMenuOptim.Application.Dtos.Common;
global using SmartMenuOptim.Application.Dtos.Customer;
global using SmartMenuOptim.Application.Dtos.Dish;
global using SmartMenuOptim.Application.Dtos.Review;
global using SmartMenuOptim.Application.Dtos.Sales;
global using SmartMenuOptim.Application.Features.Restaurants.DTOs;

// Type aliases for backward compatibility
global using UserBaseDTO = SmartMenuOptim.Application.Dtos.Common.UserBaseDTO;
global using AdminUserDTO = SmartMenuOptim.Application.Dtos.Admin.AdminUserDTO;
global using BusinessRuleDTO = SmartMenuOptim.Application.Dtos.Admin.BusinessRuleDTO;
global using CustomerDTO = SmartMenuOptim.Application.Dtos.Customer.CustomerDTO;
global using DishDTO = SmartMenuOptim.Application.Dtos.Dish.DishDTO;
global using CategoryDTO = SmartMenuOptim.Application.Dtos.Dish.CategoryDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Dtos.Dish.UnderperformingDishDTO;
global using ReviewDTO = SmartMenuOptim.Application.Dtos.Review.ReviewDTO;
global using SaleRecordDTO = SmartMenuOptim.Application.Dtos.Sales.SaleRecordDTO;
global using CategoryGroupDTO = SmartMenuOptim.Application.Dtos.Sales.CategoryGroupDTO;
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationRequestDTO;
global using AiRecommendationResponseDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationResponseDTO;
global using InsightResponseDTO = SmartMenuOptim.Application.Dtos.AI.InsightResponseDTO;

// Legacy aliases (for old naming with typos)
global using AiRecomendationRequestDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationRequestDTO;
global using AiRecomendationResponseDTO = SmartMenuOptim.Application.Dtos.AI.AiRecommendationResponseDTO;
