namespace Game.Combat
{
    public static class BodyParts
    {
        public static readonly BodyPart[] All =
        {
            BodyPart.RightHand, BodyPart.LeftHand, BodyPart.Body, BodyPart.RightLeg, BodyPart.LeftLeg,
        };

        public static EquipSlot SlotFor(BodyPart part)
        {
            switch (part)
            {
                case BodyPart.RightHand:
                case BodyPart.LeftHand:
                    return EquipSlot.Hand;
                case BodyPart.Body:
                    return EquipSlot.Body;
                default:
                    return EquipSlot.Leg;
            }
        }

        public static string ShortName(BodyPart part)
        {
            switch (part)
            {
                case BodyPart.RightHand: return "R.Hand";
                case BodyPart.LeftHand: return "L.Hand";
                case BodyPart.Body: return "Body";
                case BodyPart.RightLeg: return "R.Leg";
                default: return "L.Leg";
            }
        }
    }
}
