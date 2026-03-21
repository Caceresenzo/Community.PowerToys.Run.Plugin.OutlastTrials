using System;
using System.Collections.Generic;
using ManagedCommon;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

/// <summary>
/// Main class of this plugin that implement all used interfaces.
/// </summary>
public class Main : IPlugin, IContextMenu, IDisposable
{
    /// <summary>
    /// ID of the plugin.
    /// </summary>
    public static string PluginID => "A91B923FD7F44BD49A0B045B0967D6E4";

    /// <summary>
    /// Name of the plugin.
    /// </summary>
    public string Name => "Outlast Trials Utilities";

    /// <summary>
    /// Description of the plugin.
    /// </summary>
    public string Description => "Small utilities for The Outlast Trials game.";

    private PluginInitContext Context { get; set; }

    private string IconPath { get; set; }

    private bool Disposed { get; set; }

    private OverlayManager _currentOverlayManager { get; set; }

    /// <summary>
    /// Return a filtered list, based on the given query.
    /// </summary>
    /// <param name="query">The query to filter the list.</param>
    /// <returns>A filtered list, can be empty when nothing was found.</returns>
    public List<Result> Query(Query query)
    {
        var search = query.Search.TrimEnd();

        if (search == "")
        {
            return
            [
                new Result
                {
                    QueryTextDisplay = "shock ",
                    IcoPath = IconPath,
                    Title = "Shock",
                    SubTitle = "Timer for Cold-snap and Toxic-shock",
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} shock ", true);
                        return false;
                    },
                    ContextData = search,
                },
                new Result
                {
                    QueryTextDisplay = "pump ",
                    IcoPath = IconPath,
                    Title = "Pumps Puzzle",
                    SubTitle = "Calculator \"Kill the Politician\" Trials's Pumps Puzzle",
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} pump ", true);
                        return false;
                    },
                    ContextData = search,
                },
            ];
        }

        if (search.StartsWith("shock"))
        {
            List<Result> results = [];

            if (_currentOverlayManager != null)
            {
                results.AddRange([
                    new Result
                    {
                        DisableUsageBasedScoring = true,
                        QueryTextDisplay = "shock sync cycle",
                        IcoPath = IconPath,
                        Title = "Synchronize at Cycle",
                        SubTitle = "Reset to the playing time",
                        Action = _ =>
                        {
                            if (_currentOverlayManager == null)
                                return false;

                            _currentOverlayManager.Sync(OverlayManager.ShockPhase.Playing);

                            return true;
                        },
                    },
                    new Result
                    {
                        DisableUsageBasedScoring = true,
                        QueryTextDisplay = "shock sync callout",
                        IcoPath = IconPath,
                        Title = "Synchronize at Callout",
                        SubTitle = "Reset to the hide time",
                        Action = _ =>
                        {
                            if (_currentOverlayManager == null)
                                return false;

                            _currentOverlayManager.Sync(OverlayManager.ShockPhase.Hiding);

                            return true;
                        },
                    },
                    new Result
                    {
                        DisableUsageBasedScoring = true,
                        QueryTextDisplay = "shock remove",
                        IcoPath = IconPath,
                        Title = "Remove the timer",
                        SubTitle = "Stop the timer",
                        Action = _ =>
                        {
                            _currentOverlayManager?.Dispose();
                            _currentOverlayManager = null;

                            Context.API.ChangeQuery(query.RawQuery, true);

                            return true;
                        },
                    },
                ]);
            }

            foreach (var shock in new[] { ShockDefinition.Toxic, ShockDefinition.Cold })
            {
                results.Add(
                    new Result
                    {
                        DisableUsageBasedScoring = true,
                        QueryTextDisplay = $"shock start {shock.Name}",
                        IcoPath = shock.IconPath,
                        Title = shock.DisplayName,
                        SubTitle =
                            $"Play for {shock.PlayingTime / 60}m{shock.PlayingTime % 60}s, hide for {shock.HidingTime}s",
                        Action = _ =>
                        {
                            _currentOverlayManager?.Dispose();

                            _currentOverlayManager = new OverlayManager(shock);
                            _currentOverlayManager.Show();

                            Context.API.ChangeQuery(query.RawQuery, true);

                            return true;
                        },
                    }
                );
            }

            return results;
        }

        if (search.StartsWith("pump"))
        {
            List<Result> results =
            [
                new Result
                {
                    DisableUsageBasedScoring = true,
                    QueryTextDisplay = "pump ",
                    IcoPath = IconPath,
                    Title = "<beginning> <first> <second> <third> <final>",
                    SubTitle = "Enter all 5 values to calculate the actions...",
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} pump ", true);
                        return false;
                    },
                },
            ];

            var remaining = search[4..].Trim();
            var parts = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 5)
            {
                var beginning = int.TryParse(parts[0], out var _b) ? _b : 0;
                var first = int.TryParse(parts[1], out var _f) ? _f : 0;
                var second = int.TryParse(parts[2], out var _s) ? _s : 0;
                var third = int.TryParse(parts[3], out var _t) ? _t : 0;
                var final = int.TryParse(parts[4], out var _l) ? _l : 0;

                var actions = PumpsPuzzleSolver.Solve(beginning, first, second, third, final);

                if (actions != null)
                {
                    var text = "";
                    text += $"{beginning}    ";
                    text += $"{PumpsPuzzleSolver.ToText(actions.First)} {first}    ";
                    text += $"{PumpsPuzzleSolver.ToText(actions.Second)} {second}    ";
                    text += $"{PumpsPuzzleSolver.ToText(actions.Third)} {third}    ";
                    text += $" = {final}";

                    results.Insert(
                        0,
                        new Result
                        {
                            DisableUsageBasedScoring = true,
                            QueryTextDisplay = "pump ",
                            IcoPath = IconPath,
                            Title = text,
                            SubTitle = $"Total actions: {actions.ActionCount}",
                            Action = _ =>
                            {
                                Context.API.ChangeQuery($"{query.ActionKeyword} pump ", true);
                                return false;
                            },
                        }
                    );
                }
                else
                {
                    results.Insert(
                        0,
                        new Result
                        {
                            DisableUsageBasedScoring = true,
                            QueryTextDisplay = "pump ",
                            IcoPath = IconPath,
                            Title = "No solution found",
                            SubTitle = "Try different values",
                            Action = _ =>
                            {
                                Context.API.ChangeQuery($"{query.ActionKeyword} pump ", true);
                                return false;
                            },
                        }
                    );
                }
            }

            return results;
        }

        return [];
    }

    /// <summary>
    /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
    public void Init(PluginInitContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(Context.API.GetCurrentTheme());

        Logger.InitializeLogger("\\PowerToys Run\\Logs\\OutlastTrials");
    }

    /// <summary>
    /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
    /// </summary>
    /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
    /// <returns>A list context menu entries.</returns>
    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        return [];
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Wrapper method for <see cref="Dispose()"/> that dispose additional objects and events form the plugin itself.
    /// </summary>
    /// <param name="disposing">Indicate that the plugin is disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Disposed || !disposing)
        {
            return;
        }

        if (Context?.API != null)
        {
            Context.API.ThemeChanged -= OnThemeChanged;
        }

        Disposed = true;
    }

    private void UpdateIconPath(Theme theme) =>
        IconPath =
            theme == Theme.Light || theme == Theme.HighContrastWhite
                ? "Images/outlasttrials.light.png"
                : "Images/outlasttrials.dark.png";

    private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
}
