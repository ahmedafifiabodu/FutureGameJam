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

        // Weapon parameters
        public const string Reloading = "reloading";
        public const string Aiming = "aiming";
        public const string MeleeAttack = "meeleAttack";
        public const string ChangingWeapon = "changingWeapon";

        // Enemy AI parameters
        public const string IsMoving = "IsMoving";
        public const string IsChasing = "IsChasing";
        public const string Attack = "Attack";
        public const string Stagger = "Stagger";
        public const string Death = "Death";
        public const string Jump = "Jump";
    }
}