using System;
using System.Linq;
using LovedLabels.Framework;
using Microsoft.Xna.Framework;
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
        /// <summary>The label to display for a petted animal.</summary>
        private string PettedLabel;

        /// <summary>The label to display for a non-petted animal.</summary>
        private string NotPettedLabel;

        /// <summary>The texture used to display a heart.</summary>
        private Texture2D Hearts;

        /// <summary>The current tooltip message to show.</summary>
        private string HoverText;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // read texture
            this.Hearts = helper.Content.Load<Texture2D>("assets/hearts.png");

            // hook up events
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.PettedLabel = this.Helper.Translation.Get("label.petted");
            this.NotPettedLabel = this.Helper.Translation.Get("label.not-petted");
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Game1.currentLocation.IsFarm)
                return;

            // reset tooltip
            this.HoverText = null;

            // get context
            GameLocation location = Game1.currentLocation;
            Vector2 mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            // show animal tooltip
            {
                // find animals
                FarmAnimal[] animals = new FarmAnimal[0];
                if (location is AnimalHouse house)
                    animals = house.animals.Values.ToArray();
                else if (location is Farm farm)
                    animals = farm.animals.Values.ToArray();

                // show tooltip
                foreach (FarmAnimal animal in animals)
                {
                    // Following values could use tweaking, no idea wtf is going on here
                    RectangleF animalBoundaries = new RectangleF(animal.position.X, animal.position.Y - animal.Sprite.getHeight(), animal.Sprite.getWidth() * 3 + animal.Sprite.getWidth() / 1.5f, animal.Sprite.getHeight() * 4);
                    if (animalBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                        this.HoverText = animal.wasPet.Value ? this.PettedLabel : this.NotPettedLabel;
                }
            }

            // show pet tooltip
            foreach (Pet pet in location.characters.OfType<Pet>())
            {
                // Following values could use tweaking, no idea wtf is going on here
                RectangleF petBoundaries = new RectangleF(pet.position.X, pet.position.Y - pet.Sprite.getHeight() * 2, pet.Sprite.getWidth() * 3 + pet.Sprite.getWidth() / 1.5f, pet.Sprite.getHeight() * 4);

                if (petBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                {
                    bool wasPet = this.Helper.Reflection.GetField<bool>(pet, "wasPetToday").GetValue();
                    this.HoverText = wasPet ? this.PettedLabel : this.NotPettedLabel;
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (Context.IsPlayerFree && this.HoverText != null)
                this.DrawSimpleTooltip(Game1.spriteBatch, this.HoverText, Game1.smallFont);
        }

        /// <summary>Draw tooltip at the cursor position with the given message.</summary>
        /// <param name="b">The sprite batch to update.</param>
        /// <param name="hoverText">The tooltip text to display.</param>
        /// <param name="font">The tooltip font.</param>
        private void DrawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            Vector2 textSize = font.MeasureString(hoverText);
            int width = (int)textSize.X + this.Hearts.Width + Game1.tileSize / 2;
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
            float halfHeartSize = this.Hearts.Width * 0.5f;
            int sourceY = (hoverText == this.PettedLabel) ? 0 : 32;
            Vector2 heartPos = new Vector2(x + textSize.X + halfHeartSize, y + halfHeartSize);
            b.Draw(this.Hearts, heartPos, new Rectangle(0, sourceY, 32, 32), Color.White);
        }
    }
}
