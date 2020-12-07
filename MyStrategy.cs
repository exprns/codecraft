using Aicup2020.Model;
using System.Collections.Generic;

namespace Aicup2020
{
    public class MyStrategy
    {
        private Vec2Int? baseVec = null;
        private List<int> builders = new List<int>();
        // TODO: добавить набор "строителей", кто только строит
        public Action GetAction(PlayerView playerView, DebugInterface debugInterface)
        {
            Action result = new Action(new System.Collections.Generic.Dictionary<int, Model.EntityAction>());
            var me = GetMe(playerView).Value;
            int resourcesOnThisStep = me.Resource;
            int myId = me.Id;
            int allEat = GetAllEat(playerView, me.Id);
            int inUseEat = GetUseEat(playerView, me.Id);
            int freeEat = allEat - inUseEat;

            int baseSize = GetEntitySize(playerView, EntityType.BuilderBase);
            if (baseVec == null)
            {
                baseVec = GetCoordOfLastEntity(playerView, EntityType.BuilderBase, myId).Value;
                baseVec = new Vec2Int(baseVec.Value.X + baseSize+2, baseVec.Value.Y + baseSize+2);
            }
            foreach (var entity in playerView.Entities) {
                if (entity.PlayerId != myId) {
                    continue;
                }
                EntityProperties properties = playerView.EntityProperties[entity.EntityType];

                
                MoveAction? moveAction = null;
                BuildAction? buildAction = null;
                if (properties.CanMove) {
                    moveAction = new MoveAction(
                        new Vec2Int(playerView.MapSize - 1, playerView.MapSize - 1),
                        true,
                        true
                    );
                } else if (properties.Build != null) {
                    if(properties.Build.Value.InitHealth != properties.MaxHealth)
                    {
                        // TODO: делает починку
                    }
                    EntityType unitType = properties.Build.Value.Options[0];
                    int currentUnits = 0;
                    foreach (var otherEntity in playerView.Entities) {
                        if (otherEntity.PlayerId != null && otherEntity.PlayerId == myId
                            && otherEntity.EntityType == unitType) {
                            currentUnits++;
                        }
                    }
                    if (entity.EntityType == EntityType.BuilderBase && resourcesOnThisStep>10 && freeEat > 0 && currentUnits<15) 
                    {
                        buildAction = new BuildAction(
                            unitType,
                            new Vec2Int(entity.Position.X + properties.Size, entity.Position.Y + properties.Size - 1)
                        );
                        resourcesOnThisStep -= 10;
                    }
                    if(entity.EntityType == EntityType.MeleeBase && resourcesOnThisStep > 20 && freeEat > 0 && currentUnits < 15)
                    {
                        buildAction = new BuildAction(
                            unitType,
                            new Vec2Int(entity.Position.X + properties.Size, entity.Position.Y + properties.Size - 1)
                        );
                        resourcesOnThisStep -= 20;
                    }
                    if (entity.EntityType == EntityType.RangedBase && resourcesOnThisStep > 30 && freeEat > 0 && currentUnits < 15)
                    {
                        buildAction = new BuildAction(
                            unitType,
                            new Vec2Int(entity.Position.X + properties.Size, entity.Position.Y + properties.Size - 1)
                        ); 
                        resourcesOnThisStep -= 30;
                    }
                }
                EntityType[] validAutoAttackTargets;
                if (entity.EntityType == EntityType.BuilderUnit) {
                    validAutoAttackTargets = new EntityType[] { EntityType.Resource };
                }
                else
                {
                    validAutoAttackTargets = new EntityType[0];
                }
                if (entity.EntityType == EntityType.BuilderUnit)
                {
                    if (resourcesOnThisStep > 50 && freeEat < 5)
                    {
                        Vec2Int pointForHouse = new Vec2Int(baseVec.Value.X, baseVec.Value.Y + 1);
                        baseVec = pointForHouse;
                        result.EntityActions[entity.Id] = new EntityAction(null, new BuildAction(EntityType.House, pointForHouse), null, null);
                        resourcesOnThisStep -= 50;
                        builders.Add(entity.Id);
                        continue;
                    }
                }
                result.EntityActions[entity.Id] = new EntityAction(
                    moveAction,
                    buildAction,
                    new AttackAction(
                        null,
                        new AutoAttack(properties.SightRange, validAutoAttackTargets)
                    ),
                    null
                );
            }
            return result;
        }

        private int GetEntitySize(PlayerView playerView, EntityType ent)
        {
            EntityProperties properties = playerView.EntityProperties[ent];

            return properties.Size;
        }

        private Vec2Int? GetCoordOfLastEntity(PlayerView playerView, EntityType ent, int playerId)
        {
            Vec2Int? pos = null;
            foreach (var entity in playerView.Entities)
            {
                if (entity.PlayerId != playerId)
                    continue;
                if(entity.EntityType == ent)
                    pos =  entity.Position;
            }
            return pos;
        }

        private int GetAllEat(PlayerView playerView, int playerId)
        {
            int eat = 0;
            foreach (var entity in playerView.Entities)
            {
                if (entity.PlayerId != playerId)
                    continue;
                EntityProperties properties = playerView.EntityProperties[entity.EntityType];
                if (properties.Build != null)
                    eat += properties.PopulationProvide;
            }
            return eat;
        }

        private int GetUseEat(PlayerView playerView, int playerId)
        {
            int eat = 0;
            foreach (var entity in playerView.Entities)
            {
                if (entity.PlayerId != playerId)
                    continue;
                EntityProperties properties = playerView.EntityProperties[entity.EntityType];
                if (properties.Build == null || entity.EntityType == EntityType.BuilderUnit)
                    eat += properties.PopulationUse;
            }
            return eat;
        }

        private Player? GetMe(PlayerView playerView)
        {
            int myId = playerView.MyId;
            for(int i = 0; i < playerView.Players.Length; i++)
            {
                if (playerView.Players[i].Id == myId)
                    return playerView.Players[i];
            }
            return null;
        }

        public void DebugUpdate(PlayerView playerView, DebugInterface debugInterface) 
        {
            debugInterface.Send(new DebugCommand.Clear());
            debugInterface.GetState();
        }
    }
}