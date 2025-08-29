using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace EcoFashionBackEnd.Services
{
    public class UserAddressService
    {
        private readonly IRepository<UserAddress, int> _userAddressRepository;
        private readonly IRepository<User, int> _userRepository;

        public UserAddressService(
            IRepository<UserAddress, int> userAddressRepository,
            IRepository<User, int> userRepository)
        {
            _userAddressRepository = userAddressRepository;
            _userRepository = userRepository;
        }

        public async Task<ApiResult<List<UserAddress>>> GetUserAddressesAsync(int userId)
        {
            try
            {
                var addresses = await _userAddressRepository.GetAll()
                    .Where(ua => ua.UserId == userId)
                    .OrderByDescending(ua => ua.IsDefault)
                    .ThenBy(ua => ua.AddressId)
                    .ToListAsync();

                return ApiResult<List<UserAddress>>.Succeed(addresses);
            }
            catch (Exception ex)
            {
                return ApiResult<List<UserAddress>>.Fail($"Error getting user addresses: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserAddress>> GetAddressByIdAsync(int addressId, int userId)
        {
            try
            {
                var address = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.AddressId == addressId && ua.UserId == userId);

                if (address == null)
                {
                    return ApiResult<UserAddress>.Fail("Address not found");
                }

                return ApiResult<UserAddress>.Succeed(address);
            }
            catch (Exception ex)
            {
                return ApiResult<UserAddress>.Fail($"Error getting address: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserAddress>> CreateAddressAsync(int userId, UserAddress address)
        {
            try
            {
                // Verify user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResult<UserAddress>.Fail("User not found");
                }

                // Set the userId
                address.UserId = userId;

                // If this is set as default, remove default from other addresses
                if (address.IsDefault)
                {
                    await RemoveDefaultFromOtherAddressesAsync(userId);
                }
                // If this is the first address for the user, make it default
                else
                {
                    var existingCount = await _userAddressRepository.GetAll()
                        .CountAsync(ua => ua.UserId == userId);
                    
                    if (existingCount == 0)
                    {
                        address.IsDefault = true;
                    }
                }

                await _userAddressRepository.AddAsync(address);
                await _userAddressRepository.Commit();

                return ApiResult<UserAddress>.Succeed(address);
            }
            catch (Exception ex)
            {
                return ApiResult<UserAddress>.Fail($"Error creating address: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserAddress>> UpdateAddressAsync(int addressId, int userId, UserAddress updatedAddress)
        {
            try
            {
                var existingAddress = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.AddressId == addressId && ua.UserId == userId);

                if (existingAddress == null)
                {
                    return ApiResult<UserAddress>.Fail("Address not found");
                }

                // Update properties
                // Cập nhật các trường địa chỉ, đổi ZipCode -> PersonalPhoneNumber
                existingAddress.AddressLine = updatedAddress.AddressLine;
                existingAddress.City = updatedAddress.City;
                existingAddress.District = updatedAddress.District;
                existingAddress.PersonalPhoneNumber = updatedAddress.PersonalPhoneNumber;
                existingAddress.Country = updatedAddress.Country;

                // Handle default status change
                if (updatedAddress.IsDefault && !existingAddress.IsDefault)
                {
                    await RemoveDefaultFromOtherAddressesAsync(userId);
                    existingAddress.IsDefault = true;
                }

                _userAddressRepository.Update(existingAddress);
                await _userAddressRepository.Commit();

                return ApiResult<UserAddress>.Succeed(existingAddress);
            }
            catch (Exception ex)
            {
                return ApiResult<UserAddress>.Fail($"Error updating address: {ex.Message}");
            }
        }

        public async Task<ApiResult<object>> DeleteAddressAsync(int addressId, int userId)
        {
            try
            {
                var address = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.AddressId == addressId && ua.UserId == userId);

                if (address == null)
                {
                    return ApiResult<object>.Fail("Address not found");
                }

                var wasDefault = address.IsDefault;

                _userAddressRepository.Remove(address);
                await _userAddressRepository.Commit();

                // If we deleted the default address, set another one as default
                if (wasDefault)
                {
                    await SetFirstAddressAsDefaultAsync(userId);
                }

                return ApiResult<object>.Succeed(new { message = "Address deleted successfully" });
            }
            catch (Exception ex)
            {
                return ApiResult<object>.Fail($"Error deleting address: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserAddress>> SetDefaultAddressAsync(int addressId, int userId)
        {
            try
            {
                var address = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.AddressId == addressId && ua.UserId == userId);

                if (address == null)
                {
                    return ApiResult<UserAddress>.Fail("Address not found");
                }

                if (address.IsDefault)
                {
                    return ApiResult<UserAddress>.Succeed(address);
                }

                // Remove default from other addresses
                await RemoveDefaultFromOtherAddressesAsync(userId);

                // Set this address as default
                address.IsDefault = true;
                _userAddressRepository.Update(address);
                await _userAddressRepository.Commit();

                return ApiResult<UserAddress>.Succeed(address);
            }
            catch (Exception ex)
            {
                return ApiResult<UserAddress>.Fail($"Error setting default address: {ex.Message}");
            }
        }

        public async Task<ApiResult<UserAddress>> GetDefaultAddressAsync(int userId)
        {
            try
            {
                var defaultAddress = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.IsDefault);

                if (defaultAddress == null)
                {
                    // If no default address, get the first one
                    defaultAddress = await _userAddressRepository.GetAll()
                        .Where(ua => ua.UserId == userId)
                        .OrderBy(ua => ua.AddressId)
                        .FirstOrDefaultAsync();
                }

                if (defaultAddress == null)
                {
                    return ApiResult<UserAddress>.Fail("No addresses found for user");
                }

                return ApiResult<UserAddress>.Succeed(defaultAddress);
            }
            catch (Exception ex)
            {
                return ApiResult<UserAddress>.Fail($"Error getting default address: {ex.Message}");
            }
        }

        public async Task<string> GetFormattedAddressAsync(int addressId, int userId)
        {
            try
            {
                var address = await _userAddressRepository.GetAll()
                    .FirstOrDefaultAsync(ua => ua.AddressId == addressId && ua.UserId == userId);

                if (address == null)
                {
                    return string.Empty;
                }

                return FormatAddress(address);
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetDefaultFormattedAddressAsync(int userId)
        {
            try
            {
                var result = await GetDefaultAddressAsync(userId);
                if (result.Success && result.Result != null)
                {
                    return FormatAddress(result.Result);
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task RemoveDefaultFromOtherAddressesAsync(int userId)
        {
            var defaultAddresses = await _userAddressRepository.GetAll()
                .Where(ua => ua.UserId == userId && ua.IsDefault)
                .ToListAsync();

            foreach (var addr in defaultAddresses)
            {
                addr.IsDefault = false;
                _userAddressRepository.Update(addr);
            }
        }

        private async Task SetFirstAddressAsDefaultAsync(int userId)
        {
            var firstAddress = await _userAddressRepository.GetAll()
                .Where(ua => ua.UserId == userId)
                .OrderBy(ua => ua.AddressId)
                .FirstOrDefaultAsync();

            if (firstAddress != null)
            {
                firstAddress.IsDefault = true;
                _userAddressRepository.Update(firstAddress);
                await _userAddressRepository.Commit();
            }
        }

        private static string FormatAddress(UserAddress address)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address.AddressLine))
                parts.Add(address.AddressLine);

            if (!string.IsNullOrWhiteSpace(address.District))
                parts.Add(address.District);

            if (!string.IsNullOrWhiteSpace(address.City))
                parts.Add(address.City);

            if (!string.IsNullOrWhiteSpace(address.Country))
                parts.Add(address.Country);

            return string.Join(", ", parts);
        }
    }
}