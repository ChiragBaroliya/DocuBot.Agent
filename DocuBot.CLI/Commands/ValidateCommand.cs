using DocuBot.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DocuBot.CLI.Commands
{
    public class ValidateCommand
    {
        private readonly IGitService _gitService;
        private readonly ILogger<ValidateCommand> _logger;

        public ValidateCommand(IGitService gitService, ILogger<ValidateCommand> logger)
        {
            _gitService = gitService;
            _logger = logger;
        }

        public void Execute()
        {
            var branch = _gitService.GetCurrentBranch();
            var isValid = _gitService.ValidateBranch(branch);
            if (isValid)
            {
                _logger.LogInformation("Branch name is valid: {Branch}", branch);
                Console.WriteLine("Branch valid");
            }
            else
            {
                _logger.LogWarning("Branch name is invalid: {Branch}", branch);
                Console.WriteLine("Branch invalid");
            }
        }
    }
}
