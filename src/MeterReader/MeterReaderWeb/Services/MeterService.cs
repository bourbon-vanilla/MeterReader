using Grpc.Core;
using MeterReaderWeb.Data;
using MeterReaderWeb.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeterReaderWeb.Services
{
    public class MeterService : MeaterReadingService.MeaterReadingServiceBase
    {
        private readonly ILogger<MeterService> _logger;
        private readonly IReadingRepository _readingRepository;

        public MeterService(ILogger<MeterService> logger, IReadingRepository readingRepository)
        {
            _logger = logger;
            _readingRepository = readingRepository;
        }

        public async override Task<StatusMessage> AddReading(ReadingPaket request, ServerCallContext context)
        {
            var result = new StatusMessage { Success = ReadingStatus.Failure };

            if (request.Successful == ReadingStatus.Success)
            {
                try
                {
                    foreach(var r in request.Readings)
                    {
                        var reading = new MeterReading
                        {
                            Value = r.ReadingValue,
                            ReadingDate = r.ReadingTime.ToDateTime(),
                            CustomerId = r.CustomerId
                        };

                        _readingRepository.AddEntity(reading);

                        if (await _readingRepository.SaveAllAsync())
                        {
                            _logger.LogInformation($"Stored {request.Readings.Count} new readings...");
                            result.Success = ReadingStatus.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Message = "Exception thrown during process";
                    _logger.LogError($"Xception during add reading action: {ex}");
                }
            }

            return result;
        }
    }
}
