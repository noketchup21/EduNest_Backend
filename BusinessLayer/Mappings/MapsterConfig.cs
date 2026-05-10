using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.User;
using DataAccessLayer.Entities;
using Mapster;

namespace BusinessLayer.Mappings
{
    public static class MapsterConfig
    {
        public static void Configure()
        {
            // ── RegisterUserDTO → User ────────────────────────────────────────
            TypeAdapterConfig<RegisterUserDTO, User>.NewConfig()
                .Ignore(dest => dest.UserId)
                .Ignore(dest => dest.Password)   // BCrypt hashed in service
                .Ignore(dest => dest.CreatedAt)  // set in service
                .Ignore(dest => dest.IsActive)   // set in service
                .Ignore(dest => dest.IsDeleted)  // set in service
                .Ignore(dest => dest.Tutor)      // navigation property
                .Ignore(dest => dest.Parent)     // navigation property
                .Ignore(dest => dest.Student);   // navigation property

            // ── User → UserResponseDTO ────────────────────────────────────────
            // No config needed — all property names match exactly
            // UserId, Name, Email, Role, Phone, CreatedAt, IsActive ✅

            // ── UserUpdateDto → User ──────────────────────────────────────────
            TypeAdapterConfig<UserUpdateDto, User>.NewConfig()
                .IgnoreNullValues(true)          // only update provided fields
                .Ignore(dest => dest.Tutor)      // navigation property
                .Ignore(dest => dest.Parent)     // navigation property
                .Ignore(dest => dest.Student);   // navigation property

            // ── LoginUserDTO ──────────────────────────────────────────────────
            // No mapping needed — only used to read credentials
        }
    }
}
