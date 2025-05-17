namespace Prefect;

internal sealed class RepoShouldHaveProjectName : Rule
{
    public override string Description => "Repo should have friendly project name.";

    public override string? Validate(Repo repo)
        => repo.HasValidProjectName ? null : "Could not determine the project name for the repo.";
}
