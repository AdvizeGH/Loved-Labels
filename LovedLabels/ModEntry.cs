using System;
using System.Linq;
using LovedLabels.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Network;

namespace LovedLabels
{
    /// <summary>The mod entry class.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration.</summary>
        private LoveLabelConfig _config;

        /// <summary>The texture used to display a heart.</summary>
        private Texture2D _hearts;

        /// <summary>The current tooltip message to show.</summary>
        private string _hoverText;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // read config
            _config = helper.ReadConfig<LoveLabelConfig>();

            // read texture
            _hearts = helper.Content.Load<Texture2D>("assets/hearts.png");

            // hook up events
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.Display.Rendered += OnRendered;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The event called when the game is updating (roughly 60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked (object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Game1.currentLocation.IsFarm)
                return;

            // reset tooltip
            _hoverText = null;

            // get context
            var location = Game1.currentLocation;
            var mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            // show animal tooltip
            {
                // find animals
                var animals = new FarmAnimal[0];
                switch (location)
                {
                    case AnimalHouse house:
                        animals = house.animals.Values.ToArray();
                        break;
                    case Farm farm:
                        animals = farm.animals.Values.ToArray();
                        break;
                }

                // show tooltip
                foreach (var animal in animals)
                {
                    // Following values could use tweaking, no idea wtf is going on here
                    var animalBoundaries = new RectangleF(animal.position.X, animal.position.Y - animal.Sprite.getHeight(), animal.Sprite.getWidth() * 3 + animal.Sprite.getWidth() / 1.5f, animal.Sprite.getHeight() * 4);
                    if (animalBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize))
                        _hoverText = animal.wasPet.Value ? _config.AlreadyPettedLabel : _config.NeedsToBePettedLabel;
                }
            }

            // show pet tooltip
            foreach (var pet in location.characters.OfType<Pet>())
            {
                // Following values could use tweaking, no idea wtf is going on here
                var petBoundaries = new RectangleF(pet.position.X, pet.position.Y - pet.Sprite.getHeight() * 2, pet.Sprite.getWidth() * 3 + pet.Sprite.getWidth() / 1.5f, pet.Sprite.getHeight() * 4);

                if (!petBoundaries.Contains(mousePos.X * Game1.tileSize, mousePos.Y * Game1.tileSize)) continue;

                bool WasPetToday(Pet pet2)
                {
                    var lastPettedDays = Helper.Reflection.GetField<NetLongDictionary<int, NetInt>>(pet2, "lastPetDay").GetValue();
                    return lastPettedDays.Values.Any(day => day == Game1.Date.TotalDays);                }
                _hoverText = WasPetToday(pet) ? _config.AlreadyPettedLabel : _config.NeedsToBePettedLabel;
            }
        }

        /// <summary>The event called after the game draws to the screen, but before it closes the sprite batch.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered (object sender, RenderedEventArgs e)
        {
            if (Context.IsPlayerFree && _hoverText != null)
                DrawSimpleTooltip(Game1.spriteBatch, _hoverText, Game1.smallFont);
        }

        /// <summary>Draw tooltip at the cursor position with the given message.</summary>
        /// <param name="b">The sprite batch to update.</param>
        /// <param name="hoverText">The tooltip text to display.</param>
        /// <param name="font">The tooltip font.</param>
        private void DrawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            var textSize = font.MeasureString(hoverText);
            var width = (int)textSize.X + _hearts.Width + Game1.tileSize / 2;
            var height = Math.Max(60, (int)textSize.Y + Game1.tileSize / 2);
            var x = Game1.getOldMouseX() + Game1.tileSize / 2;
            var y = Game1.getOldMouseY() + Game1.tileSize / 2;
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
                var tPosVector = new Vector2(x + (Game1.tileSize / 4), y + (Game1.tileSize / 4 + 4));
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(0f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 0f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector, Game1.textColor * 0.9f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
            var halfHeartSize = _hearts.Width * 0.5f;
            var sourceY = (hoverText == _config.AlreadyPettedLabel) ? 0 : 32;
            var heartpos = new Vector2(x + textSize.X + halfHeartSize, y + halfHeartSize);
            b.Draw(_hearts, heartpos, new Rectangle(0, sourceY, 32, 32), Color.White);
        }
    }
}
