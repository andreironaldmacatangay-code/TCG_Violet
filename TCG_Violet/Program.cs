using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace TradingCardGame
{
    public enum CardType { FIRE, WATER, GRASS }
    public enum StatusEffect { None, Burned, Confused, Poisoned }

    public enum NationalIndex
    {
        Pyro = 1, Moss, Dew, Flare, Fern,
        Mist, Scorch, Vine, Tide, Cinder,
        Sprout, Ripple, Blaze, Bloom, Brook
    }

    public interface ICard
    {
        void PrintCard(int copies = 1);
    }

    public abstract class BaseCard : ICard
    {
        public string Name { get; set; }
        public CardType Type { get; set; }
        public int HP { get; set; }
        public string AttackCode { get; set; }
        public int Attack { get; set; }
        public int Rarity { get; set; }
        public string SpecialMove { get; set; }

        public abstract void PrintCard(int copies = 1);
    }

    public class FireCard : BaseCard
    {
        public override void PrintCard(int copies = 1)
            => CardPrinter.Print(this, copies);
    }

    public class WaterCard : BaseCard
    {
        public override void PrintCard(int copies = 1)
            => CardPrinter.Print(this, copies);
    }

    public class GrassCard : BaseCard
    {
        public override void PrintCard(int copies = 1)
            => CardPrinter.Print(this, copies);
    }

    public static class CardPrinter
    {
        public static void Print(BaseCard card, int copies)
        {
            int innerWidth = 18;

            char horizontal = '-';
            char vertical = '|';
            char corner = '+';

            switch (card.Rarity)
            {
                case 1: horizontal = '-'; vertical = '|'; corner = '+'; break;
                case 2: horizontal = '='; vertical = '|'; corner = '+'; break;
                case 3: horizontal = '*'; vertical = '*'; corner = '*'; break;
                case 4: horizontal = '@'; vertical = '@'; corner = '@'; break;
                case 5: horizontal = '#'; vertical = '#'; corner = '#'; break;
            }

            string border = corner + new string(horizontal, innerWidth) + corner;

            string rightSide = "";
            string suffix = "";
            string topBorder;
            string bottomBorder;

            bool isStandard = card.Rarity <= 2;


            char stackHorizontal = '-';
            char stackVertical = '|';


            if (copies >= 6)
            {
                topBorder = border;
                bottomBorder = border;
            }
            else
            {
                int stackCount = copies >= 4 ? 4 :
                      copies >= 2 ? 2 : 0;

                rightSide = new string(stackVertical, stackCount);
                suffix = new string(isStandard ? '+' : horizontal, stackCount);

                topBorder = border + suffix;
                bottomBorder = border + suffix;
            }


            string Format(string text)
            {
                return $"{vertical} " +
                       text.PadRight(innerWidth - 2) +
                       $" {vertical}{rightSide}";
            }

            UI.Center(topBorder);

            if (copies >= 6)
            {
                int left = (Console.WindowWidth - topBorder.Length) / 2;
                int line = Console.CursorTop - 1;

                Console.SetCursorPosition(left + border.Length + 1, line);
                Console.Write($"x{copies}");

                Console.SetCursorPosition(0, Console.CursorTop + 1);
            }

            UI.Center(Format($"{card.Name.ToUpper().PadRight(10)} HP:{card.HP}"));
            UI.Center(Format($"TYPE: {card.Type}"));
            UI.Center(Format($"{Program.AttackNames[card.AttackCode].ToUpper()}: {card.Attack}"));
            UI.Center(Format($"{card.SpecialMove.ToUpper()}"));

            UI.Center(bottomBorder);
        }
    }

    static class UI
    {
        public static void Title(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Center(text);
            Console.ResetColor();
        }

        public static void Center(string text)
        {
            int width = Console.WindowWidth;
            int left = Math.Max((width - text.Length) / 2, 0);
            Console.WriteLine(new string(' ', left) + text);
        }

        public static void Victory(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Center(text);
            Console.ResetColor();
        }

        public static void Defeat(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Center(text);
            Console.ResetColor();
        }

        public static void Menu(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Center(text);
            Console.ResetColor();
        }

        public static void BinderDesign(string text)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Center(text);
            Console.ResetColor();
        }

        public static void SetColor(CardType type)
        {
            switch (type)
            {
                case CardType.FIRE:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case CardType.GRASS:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case CardType.WATER:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
            }
        }
    }

    class Program
    {
        static BaseCard currentPlayer;
        static List<BaseCard> allCards = new();
        static Dictionary<int, int> binder = new();
        static Random rng = new();

        public static readonly Dictionary<string, string> AttackNames =
            new()
            {
                {"JMS","Jump Scare"},{"SFB","Soft Block"},{"HPS","Hard Pass"},
                {"DLL","Delulu"},{"BRR","Brain Rot"},{"MXS","Mixed Signal"},
                {"RBD","Rebound"},{"ORB","Orbiting"},{"RLP","Relapse"},
                {"GHS","Ghosting"},{"HRL","Hard Launch"},{"NCH","Nonchalant"},
                {"LVB","Love Bomb"},{"AUF","Aura Farm"},{"TRD","Trauma Dump"}
            };

        static readonly Dictionary<CardType, (string name, StatusEffect effect)> Spells =
            new()
            {
                { CardType.FIRE, ("Burn", StatusEffect.Burned) },
                { CardType.WATER, ("Scald", StatusEffect.Confused) },
                { CardType.GRASS, ("Poison", StatusEffect.Poisoned) }
            };

        static void Main()
        {
            allCards = LoadCards("cards.txt");
            LoadBinder("pulls.txt");

            while (true)
            {
                UI.Title("=== MENU ===");
                UI.Menu("[1] Pull Cards");
                UI.Menu("[2] Battle");
                UI.Menu("[3] Display Binder");
                UI.Menu("[4] Exit");

                switch (Console.ReadLine())
                {
                    case "1": PullCards(); break;
                    case "2": StartBattle(); break;
                    case "3": DisplayBinder(); break;
                    case "4": SaveBinder("pulls.txt"); return;
                }
            }
        }

        static List<BaseCard> LoadCards(string path)
        {
            var list = new List<BaseCard>();

            foreach (var line in File.ReadAllLines(path))
            {
                var p = line.Split(',');

                BaseCard card = p[1].ToUpper() switch
                {
                    "FIRE" => new FireCard(),
                    "WATER" => new WaterCard(),
                    "GRASS" => new GrassCard(),
                    _ => null
                };

                card.Name = p[0];
                card.Type = Enum.Parse<CardType>(p[1].ToUpper());
                card.Rarity = int.Parse(p[2]);
                card.HP = int.Parse(p[3]);
                card.AttackCode = p[4];
                card.Attack = int.Parse(p[5]);
                card.SpecialMove = p[6];

                list.Add(card);
            }

            return list;
        }

        static void LoadBinder(string path)
        {
            for (int i = 1; i <= 15; i++) binder[i] = 0;

            if (!File.Exists(path)) return;

            foreach (var line in File.ReadAllLines(path))
            {
                var p = line.Split(',');
                binder[int.Parse(p[0])] = int.Parse(p[1]);
            }
        }

        static void SaveBinder(string path)
        {
            using StreamWriter sw = new(path);
            foreach (var kv in binder)
                sw.WriteLine($"{kv.Key},{kv.Value}");
        }

        static void PullCards()
        {
            UI.Menu("Pulling cards...");
            Thread.Sleep(600);

            for (int i = 0; i < 5; i++)
            {
                var card = allCards[rng.Next(allCards.Count)];
                int index = (int)Enum.Parse(typeof(NationalIndex), card.Name);

                binder[index]++;
                UI.SetColor(card.Type);
                card.PrintCard(binder[index]);
                Console.ResetColor();

                Thread.Sleep(400);
            }

            SaveBinder("pulls.txt");
            DisplayBinder();
        }

        static void DisplayBinder()
        {
            UI.Title("=== BINDER ===");

            UI.BinderDesign("+-----------------------------------------------------------------------------------------+");

            for (int row = 0; row < 3; row++)
            {
                string line = "";

                for (int col = 1; col <= 5; col++)
                {
                    int i = row * 5 + col;

                    if (binder[i] > 0)
                    {
                        string name = ((NationalIndex)i).ToString();

                        var card = allCards.First(c => c.Name == name);
                        string typeLetter = card.Type.ToString()[0].ToString();

                        line += $"| {($"{name}({typeLetter})").PadRight(15)} ";
                    }
                    else
                    {
                        line += $"| #{i:000}".PadRight(18);
                    }
                }

                line += "|";
                UI.BinderDesign(line);
                UI.BinderDesign("+-----------------------------------------------------------------------------------------+");
            }
            UI.Center("");
            UI.Menu("Legend: F=Fire  W=Water  G=Grass");
            UI.Center("");
        }

        static void AttackAnimation(BaseCard attacker)
        {
            UI.SetColor(attacker.Type);
            UI.Center($"{attacker.Name} attacking...");
            Console.ResetColor();

            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(600);
                UI.Center("");
            }

            Console.WriteLine();
        }

        static void StartBattle()
        {
            BaseCard player = null;
            BaseCard originalPlayer = null;

            while (true)
            {
                var aiCard = allCards[rng.Next(allCards.Count)];

                UI.Title("AI is choosing a card...");
                Thread.Sleep(800);

                UI.SetColor(aiCard.Type);
                aiCard.PrintCard();
                Console.ResetColor();

                UI.Center("");

                if (player == null)
                {
                    while (true)
                    {
                        DisplayBinder();
                        UI.Menu("Choose your card (1-15):");

                        if (!int.TryParse(Console.ReadLine(), out int choice) || binder[choice] == 0)
                        {
                            UI.Center("You do not have this card.");
                            Console.WriteLine();
                            continue;
                        }


                        var selected = allCards.First(c => c.Name == ((NationalIndex)choice).ToString());


                        UI.Title("=== SELECTED CARD ===");
                        UI.SetColor(selected.Type);
                        selected.PrintCard(binder[choice]);
                        Console.ResetColor();

                        UI.Center("");


                        UI.Menu("Confirm selection?");
                        UI.Menu("[1] Yes");
                        UI.Menu("[2] Choose Again");

                        string confirm = Console.ReadLine();

                        if (confirm == "1")
                        {
                            originalPlayer = selected;
                            player = Clone(originalPlayer);
                            break;
                        }
                        else if (confirm == "2")
                        {
                            continue;
                        }
                        else
                        {
                            UI.Center("Invalid input. Try again.");
                        }
                    }
                }

                var enemy = Clone(aiCard);
                player = Clone(originalPlayer);
                PlayBattle(player, enemy);

                UI.Title("=== Replay Options ===");
                UI.Menu("[1] Play again using current card");
                UI.Menu("[2] Choose another card");
                UI.Menu("[3] Exit");

                string input = Console.ReadLine();

                if (input == "1")
                {

                    continue;
                }
                else if (input == "2")
                {

                    player = null;
                    continue;
                }
                else if (input == "3")
                {
                    break;
                }
            }
        }

        static void PlayBattle(BaseCard player, BaseCard enemy)
        {
            currentPlayer = player;
            UI.Title("=== BATTLE LOG ===");
            Console.WriteLine();
            UI.Menu("[1] Toss Coin");
            UI.Menu("[2] Go First");
            UI.Menu("[3] Let AI Go First");

            string choice = Console.ReadLine();
            bool playerFirst = true;

            if (choice == "1")
            {
                UI.Title("Tossing coin...");
                Thread.Sleep(800);

                playerFirst = rng.Next(2) == 0;

                if (playerFirst)
                    UI.Menu("HEADS! You go first!\n");
                else
                    UI.Menu("TAILS! AI goes first!\n");

                Thread.Sleep(800);
            }
            else if (choice == "2")
            {
                playerFirst = true;
                UI.Menu("You chose to go first!\n");
            }
            else if (choice == "3")
            {
                playerFirst = false;
                UI.Menu("AI goes first!\n");
            }
            else
            {
                UI.Menu("Invalid choice! Defaulting to coin toss...");
                playerFirst = rng.Next(2) == 0;
            }

            Thread.Sleep(500);

            if (playerFirst)
            {
                NormalAttack(player, enemy);
                if (enemy.HP <= 0) { UI.Victory("Victory!"); return; }

                NormalAttack(enemy, player);
                if (player.HP <= 0) { UI.Defeat("Defeat!"); return; }
            }
            else
            {
                NormalAttack(enemy, player);
                if (player.HP <= 0) { UI.Defeat("Defeat!"); return; }

                NormalAttack(player, enemy);
                if (enemy.HP <= 0) { UI.Victory("Victory!"); return; }
            }

            SpecialTurn(player, enemy);
            if (enemy.HP <= 0) { UI.Victory("Victory!"); return; }

            SpecialTurn(enemy, player);
            if (player.HP <= 0) { UI.Defeat("Defeat!"); return; }

            UI.Center("Draw!");
        }

        static Action<BaseCard, BaseCard> NormalAttack =
            (attacker, defender) =>
            {
                bool isPlayer = attacker == currentPlayer;

                int damage = attacker.Attack;

                if (HasAdvantage(attacker.Type, defender.Type))
                {
                    damage += 20;
                    Console.ForegroundColor = ConsoleColor.White;
                    UI.Center("Type advantage! +20 damage");
                    Console.ResetColor();
                }

                AttackAnimation(attacker);

                defender.HP -= damage;

                UI.SetColor(attacker.Type);
                UI.Center($"{(isPlayer ? "[PLAYER]" : "[AI]")} {attacker.Name} dealt {damage} damage!");
                Console.ResetColor();

                UI.SetColor(defender.Type);
                UI.Center($"{(!isPlayer ? "[PLAYER]" : "[AI]")} {defender.Name} HP: {Math.Max(0, defender.HP)}");
                Console.ResetColor();

                UI.Center("");
            };

        static Action<BaseCard, BaseCard> SpecialTurn =
            (attacker, defender) =>
            {
                UI.SetColor(attacker.Type);
                UI.Center($"{attacker.Name}'s Special Turn!");
                Console.ResetColor();

                bool heads = rng.Next(2) == 0;
                int damage = attacker.Attack;

                if (HasAdvantage(attacker.Type, defender.Type))
                {
                    damage += 20;
                    UI.Center("Type advantage! +20 damage");
                    UI.Center("");
                }

                if (heads)
                {
                    Console.ForegroundColor = heads ? ConsoleColor.Yellow : ConsoleColor.Yellow;
                    UI.Center($"Coin Toss: {(heads ? "HEADS!" : "TAILS!")}");
                    Console.ResetColor();
                    NormalAttack(attacker, defender);


                    defender.HP -= 20;

                    var spell = Spells[attacker.Type];
                    UI.SetColor(attacker.Type);
                    UI.Center($"{spell.name}! {spell.effect} applied (-20 HP)");
                    Console.ResetColor();
                    UI.Center("");
                }
                else
                {
                    UI.Center("Coin Toss: TAILS!");
                    NormalAttack(attacker, defender);
                    UI.Center("");

                }
            };

        static bool HasAdvantage(CardType a, CardType b)
        {
            return (a == CardType.FIRE && b == CardType.GRASS) ||
                   (a == CardType.GRASS && b == CardType.WATER) ||
                   (a == CardType.WATER && b == CardType.FIRE);
        }

        static BaseCard Clone(BaseCard c)
        {
            BaseCard copy = c.Type switch
            {
                CardType.FIRE => new FireCard(),
                CardType.WATER => new WaterCard(),
                CardType.GRASS => new GrassCard(),
                _ => null
            };

            copy.Name = c.Name;
            copy.Type = c.Type;
            copy.HP = c.HP;
            copy.Attack = c.Attack;
            copy.AttackCode = c.AttackCode;
            copy.Rarity = c.Rarity;
            copy.SpecialMove = c.SpecialMove;

            return copy;
        }
    }
}
