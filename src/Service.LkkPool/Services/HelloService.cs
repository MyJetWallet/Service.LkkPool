﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.LkkPool.Grpc;
using Service.LkkPool.Grpc.Models;
using Service.LkkPool.Settings;

namespace Service.LkkPool.Services
{
    public class HelloService: IHelloService
    {
        private readonly ILogger<HelloService> _logger;

        public HelloService(ILogger<HelloService> logger)
        {
            _logger = logger;
        }

        public Task<HelloMessage> SayHelloAsync(HelloRequest request)
        {
            _logger.LogInformation("Hello from {name}", request.Name);

            return Task.FromResult(new HelloMessage
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
