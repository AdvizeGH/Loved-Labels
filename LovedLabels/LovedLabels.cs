using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace LovedLabels
{
    public class LovedLabels : Mod
    {

        private LoveLabelConfig config;
        private Texture2D hearts;
        private string hoverPetLabel;


        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<LoveLabelConfig>();
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            GameEvents.UpdateTick += Event_UpdateTick;
            GraphicsEvents.OnPostRenderHudEvent += Event_PostRenderHUDEvent;
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            try
            {
                ContentManager cm = new ContentManager(Game1.content.ServiceProvider, Helper.DirectoryPath);
                hearts = cm.Load<Texture2D>("hearts");
            }
            catch (Exception ex)
            {
                Monitor.Log("Failed to load heart texture: " + ex, LogLevel.Error);
            }
        }

        private void Event_UpdateTick(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame)
                return;
            if (!Game1.currentLocation.isFarm)
                return;

            hoverPetLabel = null;
            GameLocation currentLocation = Game1.currentLocation;
            Vector2 mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            List<FarmAnimal> animals = new List<FarmAnimal>();
            if (currentLocation is AnimalHouse)
            {
                animals = ((AnimalHouse)currentLocation).animals.Values.ToList();
            }
            else if (currentLocation is Farm)
            {
                animals = ((Farm)currentLocation).animals.Values.ToList();
            }
            foreach (FarmAnimal animal in animals)
            {
                // Following values could use tweaking, no idea wtf is going on here
                RectangleF animalBoundaries = new RectangleF(animal.position.X, animal.position.Y - animal.sprite.getHeight(), animal.sprite.getWidth() * 3 + animal.sprite.getWidth() / 1.5f, animal.sprite.getHeight() * 4);
                
                if (animalBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                {
                    hoverPetLabel = animal.wasPet ? config.AlreadyPettedLabel : config.NeedsToBePettedLabel;
                }
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
                        hoverPetLabel = wasPet ? config.AlreadyPettedLabel : config.NeedsToBePettedLabel;
                    }
                }
            }
        }

        private void Event_PostRenderHUDEvent(object sender, EventArgs e)
        {
            if (hoverPetLabel != null && Game1.activeClickableMenu == null)
            {
                drawSimpleTooltip(Game1.spriteBatch, hoverPetLabel, Game1.smallFont);
            }
        }

        private void drawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            Vector2 textSize = font.MeasureString(hoverText);
            int width = (int)textSize.X + (int)hearts.Width + Game1.tileSize / 2;
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
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White, 1f, true);
            if (hoverText.Count() > 1)
            {
                Vector2 tPosVector = new Vector2(x + (Game1.tileSize / 4), y + (Game1.tileSize / 4 + 4));
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(0f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 0f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector, Game1.textColor * 0.9f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
            float halfHeartSize = hearts.Width * 0.5f;
            int sourceY = (hoverText == config.AlreadyPettedLabel) ? 0 : 32;
            Vector2 heartpos = new Vector2(x + textSize.X + halfHeartSize, y + halfHeartSize);
            b.Draw(hearts, heartpos, new Rectangle(0, sourceY, 32, 32), Color.White);
        }    
    }

    public class LoveLabelConfig
    {
        public string AlreadyPettedLabel { get; set; } = "Is Loved";

        public string NeedsToBePettedLabel { get; set; } = "Needs Love";

        /*public LoveLabelConfig()
        {
            AlreadyPettedLabel = "Is Loved";
            NeedsToBePettedLabel = "Needs Love";
        }*/
    }

    public class RectangleF
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(float xPos, float yPos)
        {
            return this.X <= xPos && xPos < this.X + this.Width && this.Y <= yPos && yPos < this.Y + this.Height;
        }
    }

}