using FluentValidation;
using Ghuri.Domain.Entities.Ops;

namespace Ghuri.Application.Features.Files.Commands.UploadFile;

internal sealed class UploadFileValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileValidator()
    {
        // Reported under "file" - the name of the form field - so the admin
        // form can show the message right under its file picker.
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .OverridePropertyName("file");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FileObject.MaxSizeBytes)
            .WithMessage($"The file is larger than {FileObject.MaxSizeBytes / 1024 / 1024} MB.")
            .OverridePropertyName("file");
    }
}
