using System.Security.Cryptography;
using System.Text;
using Domain.DTOs.OsonSms;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace Infrastructure.Services;

public class OsonSmsService(IConfiguration configuration) : IOsonSmsService
{
    private readonly RestClient _restClient = new();
    private readonly string _login = configuration["OsonSmsSettings:Login"] ??
                                     throw new InvalidOperationException("OsonSmsSettings:Login not configured");
    private readonly string _passHash = configuration["OsonSmsSettings:PassHash"] ??
                                        throw new InvalidOperationException("OsonSmsSettings:PassHash not configured");
    private readonly string _sender = configuration["OsonSmsSettings:Sender"] ??
                                      throw new InvalidOperationException("OsonSmsSettings:Sender not configured");
    private readonly string _dlm = configuration["OsonSmsSettings:Dlm"] ??
                                   throw new InvalidOperationException("OsonSmsSettings:Dlm not configured");
    private readonly string _t = configuration["OsonSmsSettings:T"] ??
                                 throw new InvalidOperationException("OsonSmsSettings:T not configured");
    private readonly string _sendSmsUrl = configuration["OsonSmsSettings:SendSmsUrl"] ??
                                          throw new InvalidOperationException("OsonSmsSettings:SendSmsUrl not configured");
    private readonly string _checkSmsStatusUrl = configuration["OsonSmsSettings:CheckSmsStatusUrl"] ??
                                                 throw new InvalidOperationException("OsonSmsSettings:CheckSmsStatusUrl not configured");
    private readonly string _checkBalanceUrl = configuration["OsonSmsSettings:CheckBalanceUrl"] ??
                                               throw new InvalidOperationException("OsonSmsSettings:CheckBalanceUrl not configured");

    public async Task<Response<OsonSmsSendResponseDto>> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var txnId = GenerateTxnId();
            var strHash = Sha256Hash(txnId + _dlm + _login + _dlm + _sender + _dlm + phoneNumber + _dlm + _passHash);

            var request = new RestRequest(_sendSmsUrl, Method.Get);
            request.AddParameter("from", _sender);
            request.AddParameter("login", _login);
            request.AddParameter("t", _t);
            request.AddParameter("phone_number", phoneNumber);
            request.AddParameter("msg", message);
            request.AddParameter("str_hash", strHash);
            request.AddParameter("txn_id", txnId);

            var response = await _restClient.ExecuteAsync<OsonSmsSendResponseDto>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                if (response.Data.Error != null)
                {
                    return new Response<OsonSmsSendResponseDto>(HttpStatusCode.BadRequest, response.Data.Error.Message);
                }

                return new Response<OsonSmsSendResponseDto>(response.Data)
                    { Message = "SMS бо муваффақият фиристода шуд" };
            }
            else
            {
                return new Response<OsonSmsSendResponseDto>(response.StatusCode,
                    response.ErrorMessage ?? "Хатогӣ ҳангоми фиристодани SMS");
            }
        }
        catch (Exception ex)
        {
            return new Response<OsonSmsSendResponseDto>(HttpStatusCode.InternalServerError, $"Хатогӣ: {ex.Message}");
        }
    }

    public async Task<Response<OsonSmsStatusResponseDto>> CheckSmsStatusAsync(string msgId)
    {
        try
        {
            var txnId = GenerateTxnId();
            var strHash = Sha256Hash(_login + _dlm + txnId + _dlm + _passHash);

            var request = new RestRequest(_checkSmsStatusUrl, Method.Get);
            request.AddParameter("t", _t);
            request.AddParameter("login", _login);
            request.AddParameter("msg_id", msgId);
            request.AddParameter("str_hash", strHash);
            request.AddParameter("txn_id", txnId);

            var response = await _restClient.ExecuteAsync<OsonSmsStatusResponseDto>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                if (response.Data.Error != null)
                {
                    return new Response<OsonSmsStatusResponseDto>(HttpStatusCode.BadRequest,
                        response.Data.Error.Message);
                }

                return new Response<OsonSmsStatusResponseDto>(response.Data)
                    { Message = "Статуси SMS бо муваффақият гирифта шуд" };
            }
            else
            {
                return new Response<OsonSmsStatusResponseDto>(response.StatusCode,
                    response.ErrorMessage ?? "Хатогӣ ҳангоми санҷиши статуси SMS");
            }
        }
        catch (Exception ex)
        {
            return new Response<OsonSmsStatusResponseDto>(HttpStatusCode.InternalServerError, $"Хатогӣ: {ex.Message}");
        }
    }

    public async Task<Response<OsonSmsBalanceResponseDto>> CheckBalanceAsync()
    {
        try
        {
            var txnId = GenerateTxnId();
            var strHash = Sha256Hash(txnId + _dlm + _login + _dlm + _passHash);

            var request = new RestRequest(_checkBalanceUrl, Method.Get);
            request.AddParameter("t", _t);
            request.AddParameter("login", _login);
            request.AddParameter("str_hash", strHash);
            request.AddParameter("txn_id", txnId);

            var response = await _restClient.ExecuteAsync<OsonSmsBalanceResponseDto>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                if (response.Data.Error != null)
                {
                    return new Response<OsonSmsBalanceResponseDto>(HttpStatusCode.BadRequest,
                        response.Data.Error.Message);
                }

                return new Response<OsonSmsBalanceResponseDto>(response.Data)
                    { Message = "Тавозун бо муваффақият гирифта шуд" };
            }
            else
            {
                return new Response<OsonSmsBalanceResponseDto>(response.StatusCode,
                    response.ErrorMessage ?? "Хатогӣ ҳангоми санҷиши тавозун");
            }
        }
        catch (Exception ex)
        {
            return new Response<OsonSmsBalanceResponseDto>(HttpStatusCode.InternalServerError, $"Хатогӣ: {ex.Message}");
        }
    }

    private string Sha256Hash(string value)
    {
        using (SHA256 hash = SHA256.Create())
        {
            byte[] result = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in result)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    private string GenerateTxnId()
    {
        return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + "";
    }
}