"""
Analysis for the VR teach pendant trainer experiment.

Put summary.csv (written by SessionLogger) next to this file, then run:

    pip install pandas matplotlib
    python analyse.py

It produces three PNG files you can paste straight into the report,
and prints the numbers you will quote in the Results section.
"""

import os
import pandas as pd
import matplotlib.pyplot as plt

HERE = os.path.dirname(os.path.abspath(__file__))
CSV = os.path.join(HERE, "summary.csv")

METRICS = [
    ("collisions", "Collisions per run"),
    ("cycle_time_s", "Program cycle time (s)"),
    ("session_time_s", "Time to complete the task (s)"),
    ("limit_warnings", "Joint limit violations"),
]


def load():
    df = pd.read_csv(CSV)
    df.columns = [c.strip() for c in df.columns]
    df["condition"] = df["condition"].str.strip()
    return df


def is_simulated(df):
    """
    True when the file carries data_source=simulated on every row.

    SessionLogger never writes that column, so it can only be there because the
    file came from make_simulated_data.py. When it is, every figure gets stamped
    so a simulated run can never be mistaken for participant data, and the stamp
    disappears by itself once real runs replace the file.
    """
    if "data_source" not in df.columns:
        return False
    return df["data_source"].astype(str).str.strip().eq("simulated").all()


def stamp(fig, df):
    if not is_simulated(df):
        return
    fig.text(0.5, 0.5, "SIMULATED DATA", fontsize=54, color="#c0392b",
             alpha=0.16, ha="center", va="center", rotation=28,
             fontweight="bold", zorder=50)
    fig.text(0.5, 0.005,
             "Simulated data, generated to verify the analysis chain. "
             "Not participant results.",
             fontsize=9, color="#c0392b", ha="center", va="bottom")


def group_bars(df):
    """Bar chart with error bars, one panel per metric, VR trained vs sheet only."""
    fig, axes = plt.subplots(2, 2, figsize=(10, 7))

    for ax, (col, label) in zip(axes.flat, METRICS):
        means = df.groupby("condition")[col].mean()
        errs = df.groupby("condition")[col].sem()

        ax.bar(means.index, means.values, yerr=errs.values, capsize=6,
               color=["#4C72B0", "#C44E52"])
        ax.set_ylabel(label)
        ax.set_title(label)
        ax.grid(axis="y", alpha=0.3)

    title = "Effect of VR training on first time robot programmers"
    if is_simulated(df):
        title = "Analysis output on simulated data, pipeline verification"
    fig.suptitle(title)
    fig.tight_layout(rect=(0, 0.03, 1, 1))
    stamp(fig, df)
    fig.savefig(os.path.join(HERE, "fig1_group_comparison.png"), dpi=200)
    print("wrote fig1_group_comparison.png")


def learning_curve(df):
    """Cycle time against attempt number, one line per condition."""
    fig, ax = plt.subplots(figsize=(7, 4.5))

    for cond, sub in df.groupby("condition"):
        by_attempt = sub.groupby("attempt")["cycle_time_s"]
        ax.errorbar(by_attempt.mean().index, by_attempt.mean().values,
                    yerr=by_attempt.sem().values, marker="o", capsize=4, label=cond)

    ax.set_xlabel("Attempt number")
    ax.set_ylabel("Cycle time (s)")
    ax.set_title("Improvement across attempts")
    ax.set_xticks(sorted(df["attempt"].unique()))
    ax.grid(alpha=0.3)
    ax.legend()
    fig.tight_layout(rect=(0, 0.04, 1, 1))
    stamp(fig, df)
    fig.savefig(os.path.join(HERE, "fig2_learning_curve.png"), dpi=200)
    print("wrote fig2_learning_curve.png")


def completion(df):
    """Share of participants who finished the task without a collision."""
    df = df.copy()
    df["clean_pass"] = ((df["task_complete"] == 1) & (df["collisions"] == 0)).astype(int)
    rate = df.groupby("condition")["clean_pass"].mean() * 100

    fig, ax = plt.subplots(figsize=(5.5, 4))
    ax.bar(rate.index, rate.values, color=["#4C72B0", "#C44E52"])
    ax.set_ylabel("Clean completions (%)")
    ax.set_ylim(0, 100)
    ax.set_title("Task completed with zero collisions")
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout(rect=(0, 0.06, 1, 1))
    stamp(fig, df)
    fig.savefig(os.path.join(HERE, "fig3_clean_pass.png"), dpi=200)
    print("wrote fig3_clean_pass.png")


def report(df):
    if is_simulated(df):
        print("\n*** This file is SIMULATED data, not participant results. ***")
        print("*** Figures will be watermarked. Replace summary.csv with the  ***")
        print("*** real one from the save folder once you have run people.    ***")
    print("\nRuns per condition:")
    print(df["condition"].value_counts().to_string())

    print("\nMean values by condition:")
    cols = [c for c, _ in METRICS] + ["points_taught", "path_length_deg"]
    print(df.groupby("condition")[cols].mean().round(2).to_string())

    print("\nStandard deviation by condition:")
    print(df.groupby("condition")[cols].std().round(2).to_string())

    # Simple two sample t test if scipy is available.
    try:
        from scipy import stats
        conds = df["condition"].unique()
        if len(conds) == 2:
            print("\nTwo sample t tests (Welch):")
            for col, label in METRICS:
                a = df[df["condition"] == conds[0]][col].dropna()
                b = df[df["condition"] == conds[1]][col].dropna()
                if len(a) > 1 and len(b) > 1:
                    t, p = stats.ttest_ind(a, b, equal_var=False)
                    print("  {:<20} t = {:6.2f}   p = {:.4f}".format(col, t, p))
    except ImportError:
        print("\n(install scipy if you want p values: pip install scipy)")


if __name__ == "__main__":
    data = load()
    report(data)
    group_bars(data)
    learning_curve(data)
    completion(data)
    print("\nDone. Three figures written next to this script.")
