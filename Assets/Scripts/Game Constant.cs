public static class GameConstant
{
    public static class Scene
    {
        public static string Mainmenu = "1- Start";
        public static string Gameplay = "2- Game";
    }

    public static class Layers
    {
        public const string Default = "Default";
        public const string Floor = "Floor";
        public const string Wall = "Wall";
        public const string Enemy = "Enemy";
        public const string Player = "Player";
    }

    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Room = "Room";
        public const string Corridor = "Corridor";
    }

    public static class AnimationParameters
    {
        // Movement parameters
        public const string WalkSpeed = "walkSpeed";

        public const string MaxSpeed = "maxSpeed";
        public const string IsMoving = "IsMoving";
        public const string IsChasing = "IsChasing";

        // Weapon parameters
        public const string Reloading = "reloading";

        public const string Aiming = "aiming";
        public const string MeleeAttack = "meeleAttack";
        public const string ChangingWeapon = "changingWeapon";

        // Attack parameters
        public const string Attack = "Attack";

        public const string Shoot = "Shoot";

        // State parameters
        public const string Stagger = "Stagger";

        public const string Death = "Death";
        public const string Jump = "Jump";

        // Enemy specific
        public const string AnimationProjectileName = "ProjectileAttack";

        public const string IsDead = "IsDead";
        public const string IsStaggered = "IsStaggered";

        public static class Door
        {
            public const string Open = "Open";
            public const string Close = "Close";
            public const string OpenJammed = "OpenJammed";
        }
    }
}