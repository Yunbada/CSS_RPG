using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    Attack,
    Defense,
    Speed,
    Jump,
    CritDamage,
    CritRate,
    MaxHealth,
    ExpBonus,
    GoldBonus
}

public class StatModifier
{
    public float Value;
    public bool IsMultiplicative;
    public object Source;

    public StatModifier(float value, bool isMultiplicative, object source = null)
    {
        Value = value;
        IsMultiplicative = isMultiplicative;
        Source = source;
    }
}

public class Stat
{
    public float BaseValue;
    private bool isDirty = true;
    private float lastValue;

    private readonly List<StatModifier> statModifiers = new List<StatModifier>();

    public Stat(float baseValue = 100f)
    {
        BaseValue = baseValue;
    }

    public float Value
    {
        get
        {
            if (isDirty)
            {
                lastValue = CalculateFinalValue();
                isDirty = false;
            }
            return lastValue;
        }
    }

    public void AddModifier(StatModifier mod)
    {
        isDirty = true;
        statModifiers.Add(mod);
    }

    public bool RemoveModifier(StatModifier mod)
    {
        if (statModifiers.Remove(mod))
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    public bool RemoveAllModifiersFromSource(object source)
    {
        bool didRemove = false;
        for (int i = statModifiers.Count - 1; i >= 0; i--)
        {
            if (statModifiers[i].Source == source)
            {
                isDirty = true;
                didRemove = true;
                statModifiers.RemoveAt(i);
            }
        }
        return didRemove;
    }

    private float CalculateFinalValue()
    {
        float finalValue = BaseValue;
        float sumPercentAdd = 0;
        float sumPercentMult = 1f;

        // "모든 스탯은 100% 기준으로... +% 증가 (가산), *% 증가 (곱연산)"
        foreach (StatModifier mod in statModifiers)
        {
            if (mod.IsMultiplicative)
            {
                // 예: 1.1f 를 주면 10% 곱연산 증가
                sumPercentMult *= mod.Value;
            }
            else
            {
                // 예: 10f 를 주면 10% 단순가산 증가
                sumPercentAdd += mod.Value;
            }
        }

        // 최종 = 기본값 * (1 + (합연산% / 100)) * 곱연산수치
        // 기획상 Base가 100% 이므로, 실질 수치에 맞게 연산
        finalValue = (BaseValue * (1f + sumPercentAdd / 100f)) * sumPercentMult;
        return (float)Math.Round(finalValue, 2);
    }
}

public class StatSystem : MonoBehaviour
{
    public Dictionary<StatType, Stat> stats;

    private void Awake()
    {
        stats = new Dictionary<StatType, Stat>
        {
            { StatType.Attack, new Stat(100f) },
            { StatType.Defense, new Stat(100f) },
            { StatType.Speed, new Stat(100f) }, // 이동속도 기준점
            { StatType.Jump, new Stat(100f) }, // 점프력 기준점
            { StatType.CritDamage, new Stat(150f) }, // 기본 치피 150%
            { StatType.CritRate, new Stat(5f) },     // 기본 치확 5%
            { StatType.MaxHealth, new Stat(100f) },
            { StatType.ExpBonus, new Stat(100f) },
            { StatType.GoldBonus, new Stat(100f) }
        };
    }

    public float GetStat(StatType type)
    {
        if (stats.ContainsKey(type))
            return stats[type].Value;
        return 0f;
    }
}
