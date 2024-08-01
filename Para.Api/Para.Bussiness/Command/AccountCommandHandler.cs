using AutoMapper;
using Hangfire;
using MediatR;
using Para.Base.Response;
using Para.Bussiness.Cqrs;
using Para.Bussiness.Notification;
using Para.Data.Domain;
using Para.Data.UnitOfWork;
using Para.Schema;

namespace Para.Bussiness.Command
{
    public class AccountCommandHandler :
        IRequestHandler<CreateAccountCommand, ApiResponse<AccountResponse>>,
        IRequestHandler<UpdateAccountCommand, ApiResponse>,
        IRequestHandler<DeleteAccountCommand, ApiResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly RabbitMQClientService _rabbitMQClientService;

        public AccountCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, RabbitMQClientService rabbitMQClientService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _rabbitMQClientService = rabbitMQClientService;
        }

        public async Task<ApiResponse<AccountResponse>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var mapped = _mapper.Map<AccountRequest, Account>(request.Request);
            mapped.OpenDate = DateTime.Now;
            mapped.Balance = 0;
            mapped.AccountNumber = new Random().Next(1000000, 9999999);
            mapped.IBAN = $"TR{mapped.AccountNumber}97925786{mapped.AccountNumber}01";
            var saved = await _unitOfWork.AccountRepository.Insert(mapped);
            await _unitOfWork.Complete();

            var customer = await _unitOfWork.CustomerRepository.GetById(request.Request.CustomerId);
            var emailMessage = new EmailMessage
            {
                Subject = "Yeni hesap açýlýþý",
                Email = customer.Email,
                Content = $"Merhaba, {customer.FirstName} {customer.LastName}, Adýnýza {request.Request.CurrencyCode} döviz cinsi hesabýnýz açýlmýþtýr."
            };

            _rabbitMQClientService.Publish("emailQueue", emailMessage);

            var response = _mapper.Map<AccountResponse>(saved);
            return new ApiResponse<AccountResponse>(response);
        }

        public async Task<ApiResponse> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
        {
            var mapped = _mapper.Map<AccountRequest, Account>(request.Request);
            mapped.Id = request.AccountId;
            _unitOfWork.AccountRepository.Update(mapped);
            await _unitOfWork.Complete();
            return new ApiResponse();
        }

        public async Task<ApiResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.AccountRepository.Delete(request.AccountId);
            await _unitOfWork.Complete();
            return new ApiResponse();
        }
    }
}
