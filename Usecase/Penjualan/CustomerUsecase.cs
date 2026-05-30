using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{

    public interface ICustomerUsecase
    {
        Task<string> InsertDataCategory(CategoryCustomer model);

        Task<bool> UpdateMasterCustomer(Customer customer);
        Task<bool> ToggleCustomerStatus(int id);
        Task<List<CategoryCustomer>> GetAllCategory();
        Task<List<Customer>> GetAllCustomer();
        Task<bool> UpdateDataCategory(CategoryCustomer model);
        Task<bool> UpdateStatusCategory(int id, int status);

        Task<string> InsertMasterCustomer(Customer customer);
    }
    public class CustomerUsecase : ICustomerUsecase
    {
        private readonly ICategoryCustomerRepo _categoryCustomerRepo;
        private readonly IMasterCustomerRepo _masterCustomerRepo;
        public CustomerUsecase(ICategoryCustomerRepo categoryCustomerRepo,IMasterCustomerRepo masterCustomerRepo)
        {
            _categoryCustomerRepo = categoryCustomerRepo;
            _masterCustomerRepo = masterCustomerRepo;
        }

        public async Task<string> InsertMasterCustomer(Customer customer)
        {
            var result = await _masterCustomerRepo.InsertMasterCustomer(customer);

            return result ? "Insert Successfully" : "Insert Failed";
        }
        public async Task<string> InsertDataCategory(CategoryCustomer category)
        {
            var result = await _categoryCustomerRepo.InsertCategoryCust(category);
            if (result)
                return "Insert Successfully";

            return "Insert Failed";
        }
        
        public async Task<bool> UpdateStatusCategory(int id, int status)
        {
            var result = await _categoryCustomerRepo.UpdateStatusCategory(id, status);
            return result;

        }

        public async Task<List<CategoryCustomer>> GetAllCategory()
        {
            var result = await _categoryCustomerRepo.GetAllCategory();
            return result;
        }

        public async Task<List<Customer>> GetAllCustomer()
        {
            var result = await _masterCustomerRepo.GetAllCustomer();
            return result;
        }

        public async Task<bool> UpdateDataCategory(CategoryCustomer model)
        {
            var result = await _categoryCustomerRepo.UpdateCategoryCust(model);
            return result;
        }

        public async Task<bool> UpdateMasterCustomer(Customer customer)
        {
            var result = await _masterCustomerRepo.UpdateMasterCustomer(customer);
            return result;
        }

        public async Task<bool> ToggleCustomerStatus(int id)
        {
            var result = await _masterCustomerRepo.ToggleCustomerStatus(id);
            return result;
        }
    }
}
