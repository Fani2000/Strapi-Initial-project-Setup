import type { Schema, Struct } from '@strapi/strapi';

export interface CommonBadge extends Struct.ComponentSchema {
  collectionName: 'components_common_badges';
  info: {
    displayName: 'badge';
  };
  attributes: {
    color: Schema.Attribute.Enumeration<
      ['amber', 'green', 'red', 'slate', 'purple']
    >;
    label: Schema.Attribute.String;
  };
}

export interface CommonNutrition extends Struct.ComponentSchema {
  collectionName: 'components_common_nutritions';
  info: {
    displayName: 'nutrition';
  };
  attributes: {
    calories: Schema.Attribute.BigInteger;
    carbs_g: Schema.Attribute.Decimal;
    fat_g: Schema.Attribute.Decimal;
    fiber_g: Schema.Attribute.Decimal;
    protein_g: Schema.Attribute.Decimal;
    sodium_mg: Schema.Attribute.Decimal;
    sugar_g: Schema.Attribute.Decimal;
  };
}

export interface CommonPrice extends Struct.ComponentSchema {
  collectionName: 'components_common_prices';
  info: {
    displayName: 'price';
  };
  attributes: {
    amount: Schema.Attribute.Decimal;
    per: Schema.Attribute.Enumeration<
      [
        'one hundred gram - 100g',
        'two hundred fifty gram - 250g',
        'five hundred gram - 500g',
        'one kilogram - 1kg',
        'each',
      ]
    >;
  };
}

declare module '@strapi/strapi' {
  export module Public {
    export interface ComponentSchemas {
      'common.badge': CommonBadge;
      'common.nutrition': CommonNutrition;
      'common.price': CommonPrice;
    }
  }
}
