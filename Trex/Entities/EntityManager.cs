﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TrexRunner.Entities
{
    public class EntityManager
    {
        // allows assignment only in constuctor and here
        private readonly List<IGameEntity> _entities = new List<IGameEntity>();
        private readonly List<IGameEntity> _entitiesToAdd = new List<IGameEntity>();
        private readonly List<IGameEntity> _entitiesToRemove = new List<IGameEntity>();

        public IEnumerable<IGameEntity> Entities => new ReadOnlyCollection<IGameEntity>(_entities);

        public void Update(GameTime gameTime)
        {
            foreach(IGameEntity entity in _entities)
            {
                if (_entitiesToRemove.Contains(entity))
                    continue;

                entity.Update(gameTime);
            }
            foreach(IGameEntity entity in _entitiesToAdd)
            {
                _entities.Add(entity);
            }

            foreach (IGameEntity entity in _entitiesToRemove)
            {
                _entities.Remove(entity);
            }

            _entitiesToAdd.Clear();
            _entitiesToRemove.Clear();
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach(IGameEntity entity in _entities.OrderBy(e => e.DrawOrder))
            {
                entity.Draw(spriteBatch, gameTime);
            }
        }

        public void AddEntity(IGameEntity entity)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity), "Null cannot be added as an entity");

            // we shouldn't manipulate list while it is being iterating
            _entitiesToAdd.Add(entity);
        }

        public void RemoveEntity(IGameEntity entity)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity), "Null cannot be removed as an entity");
            
            _entitiesToRemove.Add(entity);
        }

        public void Clear()
        {
            _entitiesToRemove.AddRange(_entities);
        }


        public IEnumerable<T> GetEntitiesOfType<T>() where T : IGameEntity
        {
            return _entities.OfType<T>();
        }
    }
}
