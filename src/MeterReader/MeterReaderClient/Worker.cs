using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MeterReaderWeb.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeterReaderClient
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _config;
        private readonly ReadingFactory _readingFactory;
        private MeaterReadingService.MeaterReadingServiceClient _client = null;


        public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, IConfiguration config, ReadingFactory readingFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _readingFactory = readingFactory ?? throw new ArgumentNullException(nameof(readingFactory));
        }


        protected MeaterReadingService.MeaterReadingServiceClient Client { get
            {
                if (_client == null)
                {
                    var channelOptions = new GrpcChannelOptions()
                    {
                        LoggerFactory = _loggerFactory
                    };
                    var channel = GrpcChannel.ForAddress(
                        _config["Service:ServerUrl"], channelOptions);
                    _client = new MeaterReadingService.MeaterReadingServiceClient(channel);
                }
                return _client;
            } }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var counter = 0;
            var customerId = _config.GetValue<int>("Service:CustomerId");

            while (!stoppingToken.IsCancellationRequested)
            {
                counter++;

                if (counter % 10 == 0)
                {
                    Console.WriteLine("Send Diagnostics");
                    var stream = Client.SendDiagnostics();
                    for(var x = 0; x < 5; x++)
                    {
                        var reading = await _readingFactory.Generate(customerId);
                        await stream.RequestStream.WriteAsync(reading);
                    }

                    await stream.RequestStream.CompleteAsync();
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                

                var packet = new ReadingPaket
                {
                    Successful = ReadingStatus.Success,
                    Notes = "This is our test"
                };

                for (int i = 0; i < 5; i++)
                {
                    packet.Readings.Add(
                        await _readingFactory.Generate(customerId));
                }
                try
                {
                    var result = await Client.AddReadingAsync(packet);
                    if (result.Success == ReadingStatus.Success)
                        _logger.LogInformation("Successfully send");
                    else _logger.LogInformation("Failed to send");
                }
                catch (RpcException ex)
                {
                    if (ex.StatusCode == StatusCode.OutOfRange)
                    {
                        _logger.LogError($"{ex.Trailers}");
                    }
                    _logger.LogError("Exception thrown: {exception}", ex.ToString());
                }

                await Task.Delay(_config.GetValue<int>("Service:DelayInterval"), stoppingToken);
            }
        }
    }
}
