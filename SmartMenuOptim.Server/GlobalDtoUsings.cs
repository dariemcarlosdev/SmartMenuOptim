// Global DTO Type Aliases for SmartMenuOptim.Server
// Provides access to Application DTOs with backward compatibility

global using SmartMenuOptim.Application.Dtos;
global using SmartMenuOptim.Application.Features.AI.DTOs;
global using SmartMenuOptim.Application.Dtos.Common;
global using SmartMenuOptim.Application.Features.Admin.DTOs;
global using SmartMenuOptim.Application.Features.Customers.DTOs;
global using SmartMenuOptim.Application.Features.Reviews.DTOs;
global using SmartMenuOptim.Application.Features.Sales.DTOs;
global using SmartMenuOptim.Application.Features.Restaurants.DTOs;
global using SmartMenuOptim.Application.Features.Orders.DTOs;

// Type aliases for backward compatibility
global using UserBaseDTO = SmartMenuOptim.Application.Dtos.Common.UserBaseDTO;
global using AdminUserDTO = SmartMenuOptim.Application.Features.Admin.DTOs.AdminUserDTO;
global using BusinessRuleDTO = SmartMenuOptim.Application.Features.Admin.DTOs.BusinessRuleDTO;
global using CustomerDTO = SmartMenuOptim.Application.Features.Customers.DTOs.CustomerDTO;
global using DishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.DishDTO;
global using CategoryDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.CategoryDTO;
global using UnderperformingDishDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.UnderperformingDishDTO;
global using MenuDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.MenuDTO;
global using ReviewDTO = SmartMenuOptim.Application.Features.Reviews.DTOs.ReviewDTO;
global using SaleRecordDTO = SmartMenuOptim.Application.Features.Sales.DTOs.SaleRecordDTO;
global using CategoryGroupDTO = SmartMenuOptim.Application.Features.Sales.DTOs.CategoryGroupDTO;
global using RestaurantDTO = SmartMenuOptim.Application.Features.Restaurants.DTOs.RestaurantDTO;
global using AiRecommendationRequestDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationRequestDTO;
global using AiRecommendationResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.AiRecommendationResponseDTO;
global using InsightResponseDTO = SmartMenuOptim.Application.Features.AI.DTOs.InsightResponseDTO;

// Order DTO aliases
global using OrderDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderDTO;
global using OrderDetailDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderDetailDTO;
global using OrderItemDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderItemDTO;
global using OrderCreateDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderCreateDTO;
global using OrderItemCreateDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderItemCreateDTO;
global using OrderUpdateDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderUpdateDTO;
global using OrderStatusDTO = SmartMenuOptim.Application.Features.Orders.DTOs.OrderStatusDTO;

