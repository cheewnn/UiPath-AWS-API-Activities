# AWS API Activities for UiPath Studio
## Background
This project came as an adhoc work request from one of my directors. With the massive suite and advancement in AWS services (specifically in the AI & ML space), it became imperative that many digital solutions may interact with AWS at some point. 
Hence the requirement for a tool for sending http requests to Amazon AWS. 

## Why not use the AWS SDK?
External assemblies may not be allowed usage in some organizations. The ability to create your own Authorization header for the API request will be useful as well if you require usage of multiple AWS tools.


### Activities
#### 1. Send Http Request to AWS services 
This activity takes in the relevant parameters you would use in a typical http request and calculates the Signature required for making an AWS http request.
It will then send the request and return the http response message.

#### 2. Generate your own Authorization Header for API requests
This activity allows you to generate your own Auth header (comprising the Calculated Signature) 
You can then use the http request activity in UiPath.WebApi.Activities to send the request. 
This allows greater flexbility when you need to send in alternative content types.
