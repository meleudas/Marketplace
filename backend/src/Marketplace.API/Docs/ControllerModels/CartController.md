# CartController — приклади body

## POST `/me/cart/items`

```json
{
  "productId": 123,
  "quantity": 2
}
```

## PATCH `/me/cart/items/{itemId}`

```json
{
  "quantity": 5
}
```

## POST `/me/cart/checkout`

```json
{
  "paymentMethod": "card",
  "address": {
    "firstName": "Іван",
    "lastName": "Петренко",
    "phone": "+380501112233",
    "street": "вул. Хрещатик, 1",
    "city": "Київ",
    "state": "Київ",
    "postalCode": "01001",
    "country": "UA"
  },
  "notes": "Доставка після 18:00"
}
```
