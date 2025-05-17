namespace Prefect;

internal abstract class Rule
{
    public abstract string Description { get; }

    /// <summary>Validates this rule for the given repo</summary>
    /// <returns>The reason the validation failed or <c>null</c> when the rule passed successfully.</returns>
    public abstract string? Validate(Repo repo);

    /// <summary>Attempts to automatically fix violations of this rule</summary>
    /// <returns><c>true</c> if a fixup was applied</returns>
    /// <remarks>
    /// Applying a fixup should apply whatever initial state would've been expected for a fresh repo (which implicitly was in violation.)
    /// 
    /// This means that fixing a rule which was not violated may revert changes which were acceptable but did not match the initial state.
    /// </remarks>
    public virtual bool Fixup(Repo repo)
        => false;
}
