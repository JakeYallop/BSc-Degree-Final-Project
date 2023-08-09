const isSuccessStatusCode = (statusCode: number) => {
    return statusCode >= 200 && statusCode <= 299;
};

export default isSuccessStatusCode;
