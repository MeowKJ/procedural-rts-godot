static partial class Program
{
    private static void AssertDifficultyWavesAndOutcomes()
    {
        var easyWaveState = new GameState();
        for (var index = 0; index < 8; index++)
        {
            easyWaveState.Units.Add(Unit(100 + index, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(2300 + index * 18, 1280), UnitStance.Hold));
        }

        var easyWaveAi = new EnemyAttackWaveAi(new EnemyDifficultyProfile(
            EnemyDifficulty.Easy,
            EnemyDifficultyProfile.Easy.ProductionInitialDelay,
            EnemyDifficultyProfile.Easy.ProductionDecisionInterval,
            EnemyDifficultyProfile.Easy.DesiredHarvesters,
            EnemyDifficultyProfile.Easy.MaxQueuedItems,
            AttackInitialDelay: 0.1f,
            EnemyDifficultyProfile.Easy.AttackWaveInterval,
            EnemyDifficultyProfile.Easy.MinimumWaveUnits,
            EnemyDifficultyProfile.Easy.MaximumWaveUnits,
            AggressionRadius: float.PositiveInfinity));
        easyWaveAi.Update(easyWaveState, 0.2);

        if (ManualEnemyCombatOrders(easyWaveState) != EnemyDifficultyProfile.Easy.MaximumWaveUnits)
        {
            throw new InvalidOperationException("easy enemy attack wave should cap the number of ordered combat units");
        }

        var hardWaveState = new GameState();
        for (var index = 0; index < 10; index++)
        {
            hardWaveState.Units.Add(Unit(200 + index, UnitDesignIds.GenericInfantry, Owner.Enemy, new Vector2(2300 + index * 18, 1320), UnitStance.Hold));
        }

        var hardWaveAi = new EnemyAttackWaveAi(new EnemyDifficultyProfile(
            EnemyDifficulty.Hard,
            EnemyDifficultyProfile.Hard.ProductionInitialDelay,
            EnemyDifficultyProfile.Hard.ProductionDecisionInterval,
            EnemyDifficultyProfile.Hard.DesiredHarvesters,
            EnemyDifficultyProfile.Hard.MaxQueuedItems,
            AttackInitialDelay: 0.1f,
            EnemyDifficultyProfile.Hard.AttackWaveInterval,
            EnemyDifficultyProfile.Hard.MinimumWaveUnits,
            EnemyDifficultyProfile.Hard.MaximumWaveUnits,
            AggressionRadius: float.PositiveInfinity));
        hardWaveAi.Update(hardWaveState, 0.2);

        if (ManualEnemyCombatOrders(hardWaveState) <= ManualEnemyCombatOrders(easyWaveState))
        {
            throw new InvalidOperationException("hard enemy attack wave should order a larger combat group than easy");
        }

        var shortRadiusWaveState = new GameState();
        var shortRadiusWaveAi = new EnemyAttackWaveAi(new EnemyDifficultyProfile(
            EnemyDifficulty.Easy,
            EnemyDifficultyProfile.Easy.ProductionInitialDelay,
            EnemyDifficultyProfile.Easy.ProductionDecisionInterval,
            EnemyDifficultyProfile.Easy.DesiredHarvesters,
            EnemyDifficultyProfile.Easy.MaxQueuedItems,
            AttackInitialDelay: 0.1f,
            EnemyDifficultyProfile.Easy.AttackWaveInterval,
            EnemyDifficultyProfile.Easy.MinimumWaveUnits,
            EnemyDifficultyProfile.Easy.MaximumWaveUnits,
            AggressionRadius: 200));
        shortRadiusWaveAi.Update(shortRadiusWaveState, 0.2);

        if (shortRadiusWaveAi.WavesLaunched != 0 || ManualEnemyCombatOrders(shortRadiusWaveState) != 0)
        {
            throw new InvalidOperationException("enemy attack waves should not launch against targets outside aggression radius");
        }

        var victoryState = new GameState();
        var outcomeEvents = 0;
        victoryState.OutcomeChanged += outcome =>
        {
            if (outcome == GameOutcome.Victory)
            {
                outcomeEvents++;
            }
        };

        var enemyHq = victoryState.Buildings.First(building => building.Owner == Owner.Enemy && building.Kind == BuildingDesignIds.Headquarters);
        enemyHq.Hp = 0;
        Advance(victoryState, 0.1f);

        if (victoryState.Outcome != GameOutcome.Victory)
        {
            throw new InvalidOperationException("destroying the enemy HQ should set the game outcome to victory");
        }

        if (outcomeEvents != 1)
        {
            throw new InvalidOperationException("victory outcome should fire exactly once when enemy HQ is destroyed");
        }

        Advance(victoryState, 0.5f);
        if (outcomeEvents != 1)
        {
            throw new InvalidOperationException("victory outcome should not repeatedly fire after it has resolved");
        }

        var defeatState = new GameState();
        var defeatEvents = 0;
        defeatState.OutcomeChanged += outcome =>
        {
            if (outcome == GameOutcome.Defeat)
            {
                defeatEvents++;
            }
        };

        var playerHeadquarters = defeatState.Buildings.First(building => building.Owner == Owner.Player && building.Kind == BuildingDesignIds.Headquarters);
        playerHeadquarters.Hp = 0;
        Advance(defeatState, 0.1f);

        if (defeatState.Outcome != GameOutcome.Defeat)
        {
            throw new InvalidOperationException("destroying the player HQ should set the game outcome to defeat");
        }

        if (defeatEvents != 1)
        {
            throw new InvalidOperationException("defeat outcome should fire exactly once when player HQ is destroyed");
        }

        Advance(defeatState, 0.5f);
        if (defeatEvents != 1)
        {
            throw new InvalidOperationException("defeat outcome should not repeatedly fire after it has resolved");
        }

        Console.WriteLine("Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes");
    }
}
