using System;
using System.Collections.Generic;
using System.Linq;
using LovedLabels.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace LovedLabels
{
    /// <summary>The mod entry class.</summary>
    public class LovedLabels : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration.</summary>
        private LoveLabelConfig Config;
        
        /// <summary>The texture used to display a heart.</summary>
        private Texture2D Heart;

        /// <summary>The current tooltip message to show.</summary>
        private string HoverText;

        
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // read config
            this.Config = helper.ReadConfig<LoveLabelConfig>();

            // hook up events
            SaveEvents.AfterLoad += this.SaveEvents_AfterLoad;
            GameEvents.UpdateTick += this.Event_UpdateTick;
            GraphicsEvents.OnPostRenderHudEvent += this.Event_PostRenderHUDEvent;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The event called after the player loads a save.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            try
            {
                ContentManager cm = new ContentManager(Game1.content.ServiceProvider, Helper.DirectoryPath);
                this.Heart = cm.Load<Texture2D>("hearts");
            }
            catch (Exception ex)
            {
                Monitor.Log("Failed to load heart texture: " + ex, LogLevel.Error);
            }
        }

        /// <summary>The event called when the game is updating (roughly 60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Event_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame)
                return;
            if (!Game1.currentLocation.isFarm)
                return;

            this.HoverText = null;
            GameLocation currentLocation = Game1.currentLocation;
            Vector2 mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            List<FarmAnimal> animals = new List<FarmAnimal>();
            if (currentLocation is AnimalHouse)
                animals = ((AnimalHouse)currentLocation).animals.Values.ToList();
            else if (currentLocation is Farm)
                animals = ((Farm)currentLocation).animals.Values.ToList();

            foreach (FarmAnimal animal in animals)
            {
                // Following values could use tweaking, no idea wtf is going on here
                RectangleF animalBoundaries = new RectangleF(animal.position.X, animal.position.Y - animal.sprite.getHeight(), animal.sprite.getWidth() * 3 + animal.sprite.getWidth() / 1.5f, animal.sprite.getHeight() * 4);

                if (animalBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                    this.HoverText = animal.wasPet ? this.Config.AlreadyPettedLabel : this.Config.NeedsToBePettedLabel;
            }

            foreach (NPC npc in currentLocation.characters)
            {
                if (npc is Pet)
                {
                    // Following values could use tweaking, no idea wtf is going on here
                    RectangleF petBoundaries = new RectangleF(npc.position.X, npc.position.Y - npc.sprite.getHeight() * 2, npc.sprite.getWidth() * 3 + npc.sprite.getWidth() / 1.5f, npc.sprite.getHeight() * 4);

                    if (petBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                    {
                        //bool wasPet = (bool)typeof(Pet).GetField("wasPetToday", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(npc as Pet);
                        bool wasPet = Helper.Reflection.GetPrivateValue<bool>(npc, "wasPetToday");
                        this.HoverText = wasPet ? this.Config.AlreadyPettedLabel : this.Config.NeedsToBePettedLabel;
                    }
                }
            }
        }

        /// <summary>The event called after the game draws to the screen, but before it closes the sprite batch.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Event_PostRenderHUDEvent(object sender, EventArgs e)
        {
            if (this.HoverText != null && Game1.activeClickableMenu == null)
                this.DrawSimpleTooltip(Game1.spriteBatch, this.HoverText, Game1.smallFont);
        }

        /// <summary>Draw tooltip at the cursor position with the given message.</summary>
        /// <param name="b">The sprite batch to update.</param>
        /// <param name="hoverText">The tooltip text to display.</param>
        /// <param name="font">The tooltip font.</param>
        private void DrawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            Vector2 textSize = font.MeasureString(hoverText);
            int width = (int)textSize.X + this.Heart.Width + Game1.tileSize / 2;
            int height = Math.Max(60, (int)textSize.Y + Game1.tileSize / 2);
            int x = Game1.getOldMouseX() + Game1.tileSize / 2;
            int y = Game1.getOldMouseY() + Game1.tileSize / 2;
            if (x + width > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - width;
                y += Game1.tileSize / 4;
            }
            if (y + height > Game1.viewport.Height)
            {
                x += Game1.tileSize / 4;
                y = Game1.viewport.Height - height;
            }
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            if (hoverText.Length > 1)
            {
                Vector2 tPosVector = new Vector2(x + (Game1.tileSize / 4), y + (Game1.tileSize / 4 + 4));
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(0f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 0f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector, Game1.textColor * 0.9f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
            float halfHeartSize = this.Heart.Width * 0.5f;
            int sourceY = (hoverText == this.Config.AlreadyPettedLabel) ? 0 : 32;
            Vector2 heartpos = new Vector2(x + textSize.X + halfHeartSize, y + halfHeartSize);
            b.Draw(this.Heart, heartpos, new Rectangle(0, sourceY, 32, 32), Color.White);
        }
    }
}