namespace Game.Combat
{
    /// <summary>
    /// One usable action from the loadout, with the body part and equipment it comes from.
    /// </summary>
    public sealed class LoadoutAction
    {
        public LoadoutAction(BodyPart bodyPart, EquipmentDefinition equipment, MoveDefinition move)
        {
            BodyPart = bodyPart;
            Equipment = equipment;
            Move = move;
        }

        public BodyPart BodyPart { get; }
        public EquipmentDefinition Equipment { get; }
        public MoveDefinition Move { get; }
    }
}
