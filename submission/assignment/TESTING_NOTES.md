# Testing Notes

1. Test results:
   Total test methods: 16.
   Total test cases executed: 20.
   Passed: 20.
   Failed: 0.

2. Line coverage:
   Overall: 90.5%.
   Domain: 100%.
   Application: 95.8%.
   Infrastructure: 0%.

3. Libraries:
   Moq: Used to mock dependencies and verify repository, inventory, and notification interactions.
   AutoFixture: Used to generate test orders with customized item prices and quantities.
   Bogus: Used to generate realistic customer data with a fixed random seed.

4. No. A customer paying 999.99 EGP pays more than a customer paying 1,000 EGP because the latter receives a 100 EGP discount.

5. Potential bug:
   If notification sending fails after the order is saved, the order remains saved. This conflicts with BR-09, which states that on any failure the order must not be saved.
