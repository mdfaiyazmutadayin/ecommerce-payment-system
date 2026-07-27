using BLL.DTOs;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class UserService
    {
        private readonly IUserRepo _userRepo;

        public UserService(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<UserResponseDto> RegisterAsync(RegisterUserDto dto)
        {
            var existing = await _userRepo.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("Email already registered");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password)
            };

            _userRepo.Create(user);

            return new UserResponseDto { Id = user.Id, Name = user.Name, Email = user.Email };
        }

        public async Task<UserResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email)
                       ?? throw new Exception("Invalid email or password");

            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Invalid email or password");

            return new UserResponseDto { Id = user.Id, Name = user.Name, Email = user.Email };
        }
    }
}