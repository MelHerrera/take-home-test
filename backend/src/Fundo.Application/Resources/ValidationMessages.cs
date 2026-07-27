namespace Fundo.Application;

/// <summary>
/// Marker type used to resolve Resources/ValidationMessages.resx via IStringLocalizer;ValidationMessages;.
/// Deliberately in the project's root namespace (not .Resources) — ResourcesPath="Resources" already
/// points at the folder; namespacing the type under .Resources too causes IStringLocalizer to look for
/// "Resources.Resources.ValidationMessages" and silently fall back to returning the resource key.
/// </summary>
public class ValidationMessages
{
}
